// ---------------------------------------------------------------------------------------
//                                        ILGPU
//                           Copyright (c) 2026 ILGPU Project
//                                    www.ilgpu.net
//
// File: LibraryLaunchSiteExpander.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace ILGPUC.Roslyn.Generation;

/// <summary>
/// Exposes simple library launch wrappers in the consumer's compilation so
/// the ordinary launch-site analyzer can compile and rewrite their kernels.
/// </summary>
static class LibraryLaunchSiteExpander
{
    public static CSharpCompilation Expand(CSharpCompilation compilation)
    {
        // Radix sort emits scan calls in its generated host method. A second
        // pass exposes those calls to the ordinary scan expansion.
        for (int pass = 0; pass < 2; ++pass)
            compilation = ExpandPass(compilation);
        return compilation;
    }

    private static CSharpCompilation ExpandPass(CSharpCompilation compilation)
    {
        foreach (var tree in compilation.SyntaxTrees.ToArray())
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var expanded = new Expander(model).Visit(root);
            if (expanded != root && expanded is not null)
                compilation = compilation.ReplaceSyntaxTree(
                    tree, tree.WithRootAndOptions(expanded, tree.Options));
        }
        return compilation;
    }

    private sealed class Expander(SemanticModel model) : CSharpSyntaxRewriter
    {
        private readonly Stack<List<MemberDeclarationSyntax>> _generated = new();
        private bool _radixExpanded;

        public override SyntaxNode? VisitCompilationUnit(
            CompilationUnitSyntax node)
        {
            var updated = (CompilationUnitSyntax)base.VisitCompilationUnit(node)!;
            if (!_radixExpanded || updated.Usings.Any(usingDirective =>
                usingDirective.Name?.ToString() == "ILGPU.ScanReduce"))
                return updated;
            return updated.AddUsings(SyntaxFactory.ParseCompilationUnit(
                "using ILGPU.ScanReduce;\n").Usings[0]);
        }

        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _generated.Push([]);
            var updated = (ClassDeclarationSyntax)base.VisitClassDeclaration(node)!;
            return updated.AddMembers([.. _generated.Pop()]);
        }

        public override SyntaxNode? VisitInvocationExpression(
            InvocationExpressionSyntax node)
        {
            if (node.Expression is not MemberAccessExpressionSyntax member ||
                _generated.Count == 0 ||
                member.Expression is not IdentifierNameSyntax ||
                model.GetSymbolInfo(node).Symbol is not IMethodSymbol method ||
                method.ContainingAssembly.Name != "ILGPU")
            {
                return base.VisitInvocationExpression(node);
            }

            if (method.Name == "Sequence" &&
                method.ContainingType.ToDisplayString() ==
                    "ILGPU.Initialization.Sequencer")
            {
                return ExpandSequence(node, member, method) ??
                    base.VisitInvocationExpression(node);
            }
            if (method.Name == "Transform" &&
                method.ContainingType.ToDisplayString() ==
                    "ILGPU.Initialization.Transformer")
            {
                return ExpandTransform(node, member) ??
                    base.VisitInvocationExpression(node);
            }
            if (method.Name == "Reduce" &&
                method.ContainingType.ToDisplayString() ==
                    "ILGPU.ScanReduce.Reduction")
            {
                return ExpandReduction(node, member, method) ??
                    base.VisitInvocationExpression(node);
            }
            if (method.Name is "InclusiveScan" or "ExclusiveScan" &&
                method.ContainingType.ToDisplayString() ==
                    "ILGPU.ScanReduce.Scanner")
            {
                return ExpandScan(node, member, method) ??
                    base.VisitInvocationExpression(node);
            }
            if (method.Name == "RadixSort" &&
                method.ContainingType.ToDisplayString() ==
                    "ILGPU.RadixSort.RadixSorter")
            {
                return ExpandRadixSort(node, member, method) ??
                    base.VisitInvocationExpression(node);
            }
            if (node.ArgumentList.Arguments.Count != 2 ||
                method.Name != "Initialize" ||
                method.ContainingType.ToDisplayString() !=
                    "ILGPU.Initialization.Initializer")
            {
                return base.VisitInvocationExpression(node);
            }

            var target = node.ArgumentList.Arguments[0].Expression;
            if (!IsStableView(target))
                return base.VisitInvocationExpression(node);
            var initializer = node.ArgumentList.Arguments[1].Expression;
            var targetType = model.GetTypeInfo(target).Type;
            if (targetType is null)
                return base.VisitInvocationExpression(node);

            var valueType = method.TypeArguments[0];
            var targetTypeName = targetType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var valueTypeName = valueType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var methodName = GetMethodName("Initialize", node);
            ExpressionSyntax value;
            string parameters;
            string launchArguments;
            if (initializer is SimpleLambdaExpressionSyntax lambda &&
                lambda.Body is ExpressionSyntax body)
            {
                if (HasCaptures(body, lambda.Parameter.Identifier.Text))
                    return base.VisitInvocationExpression(node);
                value = (ExpressionSyntax)new ParameterRenamer(
                    lambda.Parameter.Identifier.Text,
                    SyntaxFactory.IdentifierName("__ilgpuIndex")).Visit(body)!;
                parameters = $"long __ilgpuIndex, {targetTypeName} target";
                launchArguments = $"__ilgpuIndex, {target}";
            }
            else if (initializer is ParenthesizedLambdaExpressionSyntax
                     { ParameterList.Parameters.Count: 1,
                       Body: ExpressionSyntax parenthesizedBody } parenthesized)
            {
                if (HasCaptures(parenthesizedBody,
                    parenthesized.ParameterList.Parameters[0].Identifier.Text))
                    return base.VisitInvocationExpression(node);
                value = (ExpressionSyntax)new ParameterRenamer(
                    parenthesized.ParameterList.Parameters[0].Identifier.Text,
                    SyntaxFactory.IdentifierName("__ilgpuIndex"))
                    .Visit(parenthesizedBody)!;
                parameters = $"long __ilgpuIndex, {targetTypeName} target";
                launchArguments = $"__ilgpuIndex, {target}";
            }
            else if (method.Parameters.Last().Type.Name == "Func")
            {
                // Delegate values cannot be compiled as device parameters.
                return base.VisitInvocationExpression(node);
            }
            else
            {
                value = SyntaxFactory.IdentifierName("value");
                parameters = $"long __ilgpuIndex, {targetTypeName} target, " +
                    $"{valueTypeName} value";
                launchArguments = $"__ilgpuIndex, {target}, {initializer}";
            }

            var generatedMethod = SyntaxFactory.ParseMemberDeclaration(
                $"private static void {methodName}({parameters}) " +
                $"{{ target[__ilgpuIndex] = ({value}); }}");
            if (generatedMethod is null)
                return base.VisitInvocationExpression(node);
            _generated.Peek().Add(generatedMethod);

            var replacement = SyntaxFactory.ParseExpression(
                $"{member.Expression}.Launch(({target}).Extent, " +
                $"__ilgpuIndex => {methodName}({launchArguments}))");
            return replacement.WithTriviaFrom(node);
        }

        private ExpressionSyntax? ExpandRadixSort(
            InvocationExpressionSyntax node,
            MemberAccessExpressionSyntax member,
            IMethodSymbol method)
        {
            // The generated kernels use a dense temporary view and specialize
            // the operation in the consumer assembly. Other overloads retain
            // the library path until their stride and pair forms are covered.
            if (node.ArgumentList.Arguments.Count != 1 ||
                method.TypeArguments.Length is not (2 or 3) ||
                (method.TypeArguments.Length == 3 &&
                    method.TypeArguments[1].Name != "Dense"))
                return null;

            var view = node.ArgumentList.Arguments[0].Expression;
            var viewType = model.GetTypeInfo(view).Type;
            if (viewType is null)
                return null;
            var viewTypeName = viewType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var valueTypeName = method.TypeArguments[0].ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var operationTypeName = method.TypeArguments[^1].ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var temporaryTypeName =
                $"global::ILGPU.Runtime.ArrayView1D<{valueTypeName}, " +
                "global::ILGPU.Stride1D.Dense>";
            var countTypeName =
                "global::ILGPU.Runtime.ArrayView1D<uint, " +
                "global::ILGPU.Stride1D.Dense>";
            var methodName = GetMethodName("RadixSort", node);
            var classifyName = methodName + "_Classify";
            var scatterName = methodName + "_Scatter";
            var classifyTempName = methodName + "_ClassifyTemp";
            var scatterTempName = methodName + "_ScatterTemp";

            MemberDeclarationSyntax? MakeClassify(string name, string sourceType) =>
                SyntaxFactory.ParseMemberDeclaration($$"""
                    private static void {{name}}(
                        long index, {{sourceType}} source,
                        {{countTypeName}} flags, int shift)
                    {
                        flags[index] =
                            {{operationTypeName}}.ExtractRadixBits(
                                source[index], shift, 1) == 0 ? 1u : 0u;
                    }
                    """);
            MemberDeclarationSyntax? MakeScatter(
                string name, string sourceType, string targetType) =>
                SyntaxFactory.ParseMemberDeclaration($$"""
                    private static void {{name}}(
                        long index, {{sourceType}} source,
                        {{targetType}} target, {{countTypeName}} prefixes,
                        int shift)
                    {
                        var value = source[index];
                        var prefix = prefixes[index];
                        var zeroCount = prefixes[source.Length - 1];
                        long destination =
                            {{operationTypeName}}.ExtractRadixBits(
                                value, shift, 1) == 0
                                ? prefix - 1L
                                : zeroCount + index - prefix;
                        target[destination] = value;
                    }
                    """);
            var host = SyntaxFactory.ParseMemberDeclaration($$"""
                private static void {{methodName}}(
                    global::ILGPU.Runtime.AcceleratorStream stream,
                    {{viewTypeName}} view)
                {
                    if (view.Length > uint.MaxValue)
                        throw new global::System.ArgumentOutOfRangeException(
                            nameof(view));
                    if ({{operationTypeName}}.NumBits < 1 ||
                        ({{operationTypeName}}.NumBits & 1) != 0)
                        throw new global::System.NotSupportedException(
                            "Radix sort requires an even positive bit count.");
                    if (view.Length < 2)
                        return;
                    using var temporary = stream.Allocate1D<{{valueTypeName}}>(
                        view.Length);
                    using var flagsData = stream.Allocate1D<uint>(view.Length);
                    using var prefixesData = stream.Allocate1D<uint>(
                        view.Length);
                    var tempView = temporary.View;
                    var flags = flagsData.View;
                    var prefixes = prefixesData.View;
                    for (int shift = 0;
                        shift < {{operationTypeName}}.NumBits; shift += 2)
                    {
                        stream.Launch(view.Extent, index =>
                            {{classifyName}}(index, view, flags, shift));
                        stream.InclusiveScan(flags, prefixes, 0u,
                            (a, b) => a + b);
                        stream.Launch(view.Extent, index =>
                            {{scatterName}}(index, view, tempView,
                                prefixes, shift));
                        int nextShift = shift + 1;
                        stream.Launch(view.Extent, index =>
                            {{classifyTempName}}(index, tempView, flags,
                                nextShift));
                        stream.InclusiveScan(flags, prefixes, 0u,
                            (a, b) => a + b);
                        stream.Launch(view.Extent, index =>
                            {{scatterTempName}}(index, tempView, view,
                                prefixes, nextShift));
                    }
                }
                """);
            var generated = new[]
            {
                MakeClassify(classifyName, viewTypeName),
                MakeScatter(scatterName, viewTypeName, temporaryTypeName),
                MakeClassify(classifyTempName, temporaryTypeName),
                MakeScatter(scatterTempName, temporaryTypeName, viewTypeName),
                host,
            };
            if (generated.Any(item => item is null))
                return null;
            _generated.Peek().AddRange(generated!);
            _radixExpanded = true;
            return SyntaxFactory.ParseExpression(
                $"{methodName}({member.Expression}, {view})")
                .WithTriviaFrom(node);
        }

        private ExpressionSyntax? ExpandScan(
            InvocationExpressionSyntax node,
            MemberAccessExpressionSyntax member,
            IMethodSymbol method)
        {
            var arguments = node.ArgumentList.Arguments;
            if (arguments.Count != 4 ||
                arguments[3].Expression is not ParenthesizedLambdaExpressionSyntax
                { ParameterList.Parameters.Count: 2,
                  Body: ExpressionSyntax body } operation ||
                HasCaptures(body,
                    operation.ParameterList.Parameters[0].Identifier.Text,
                    operation.ParameterList.Parameters[1].Identifier.Text))
            {
                return null;
            }

            var source = arguments[0].Expression;
            var target = arguments[1].Expression;
            var identity = arguments[2].Expression;
            var sourceType = model.GetTypeInfo(source).Type;
            var targetType = model.GetTypeInfo(target).Type;
            if (sourceType is null || targetType is null)
                return null;

            var sourceTypeName = sourceType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var targetTypeName = targetType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var valueTypeName = method.TypeArguments[0].ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var temporaryTypeName =
                $"global::ILGPU.Runtime.ArrayView1D<{valueTypeName}, " +
                "global::ILGPU.Stride1D.Dense>";
            var methodName = GetMethodName(method.Name, node);
            var firstPassName = methodName + "_FirstPass";
            var secondPassName = methodName + "_SecondPass";
            var operationText = operation.WithoutTrivia().ToString();
            var applyExpression = new ParameterRenamer(
                operation.ParameterList.Parameters[0].Identifier.Text,
                SyntaxFactory.IdentifierName("left"))
                .Visit(body)!;
            applyExpression = new ParameterRenamer(
                operation.ParameterList.Parameters[1].Identifier.Text,
                SyntaxFactory.IdentifierName("right"))
                .Visit(applyExpression)!;
            var applyText = applyExpression.WithoutTrivia().ToString();
            var scanMethod = method.Name == "InclusiveScan"
                ? "InclusiveScan"
                : "ExclusiveScan";

            var firstPass = SyntaxFactory.ParseMemberDeclaration($$"""
                private static void {{firstPassName}}(
                    {{sourceTypeName}} source,
                    {{temporaryTypeName}} rightBoundaries,
                    {{valueTypeName}} identity,
                    int numIterationsPerGroup)
                {
                    var prefix = identity;
                    for (int iteration = 0;
                        iteration < numIterationsPerGroup;
                        ++iteration)
                    {
                        long offset =
                            ((long)global::ILGPU.Grid.Index *
                                numIterationsPerGroup + iteration) *
                            global::ILGPU.Group.Dimension +
                            global::ILGPU.Group.Index;
                        var value = offset < source.Length
                            ? source[offset]
                            : identity;
                        var chunk = global::ILGPU.Group.AllReduce(
                            value, identity, {{operationText}});
                        var left = prefix;
                        var right = chunk;
                        prefix = ({{applyText}});
                    }
                    if (global::ILGPU.Group.IsFirstThread)
                        rightBoundaries[global::ILGPU.Grid.Index] = prefix;
                }
                """);
            var secondPass = SyntaxFactory.ParseMemberDeclaration($$"""
                private static void {{secondPassName}}(
                    {{sourceTypeName}} source,
                    {{targetTypeName}} target,
                    {{temporaryTypeName}} rightBoundaries,
                    {{valueTypeName}} identity,
                    int numIterationsPerGroup)
                {
                    var localRightBoundary =
                        global::ILGPU.Group.Index < rightBoundaries.Length
                            ? rightBoundaries[global::ILGPU.Group.Index]
                            : identity;
                    var scannedGroups = global::ILGPU.Group.ExclusiveScan(
                        localRightBoundary, identity, {{operationText}});
                    var sharedPrefix = global::ILGPU.Group.GetSharedMemory<
                        {{valueTypeName}}>(1);
                    if (global::ILGPU.Group.Index == global::ILGPU.Grid.Index)
                        sharedPrefix[0] = scannedGroups;
                    global::ILGPU.Group.Barrier();
                    var prefix = sharedPrefix[0];
                    for (int iteration = 0;
                        iteration < numIterationsPerGroup;
                        ++iteration)
                    {
                        long offset =
                            ((long)global::ILGPU.Grid.Index *
                                numIterationsPerGroup + iteration) *
                            global::ILGPU.Group.Dimension +
                            global::ILGPU.Group.Index;
                        var value = offset < source.Length
                            ? source[offset]
                            : identity;
                        var scanned = global::ILGPU.Group.{{scanMethod}}(
                            value, identity, {{operationText}});
                        if (offset < target.Length)
                        {
                            var left = prefix;
                            var right = scanned;
                            target[offset] = ({{applyText}});
                        }
                        var chunk = global::ILGPU.Group.AllReduce(
                            value, identity, {{operationText}});
                        {
                            var left = prefix;
                            var right = chunk;
                            prefix = ({{applyText}});
                        }
                    }
                }
                """);
            var host = SyntaxFactory.ParseMemberDeclaration($$"""
                private static void {{methodName}}(
                    global::ILGPU.Runtime.AcceleratorStream stream,
                    {{sourceTypeName}} source,
                    {{targetTypeName}} target,
                    {{valueTypeName}} identity)
                {
                    if (source.Length < target.Length)
                        throw new global::System.ArgumentOutOfRangeException(
                            nameof(target));
                    if (target.Length == 0)
                        return;
                    var config = stream.ComputeGridStrideKernelConfig(
                        source.Length, out int numIterationsPerGroup);
                    if (config.GridSize > config.GroupSize)
                        throw new global::System.ArgumentOutOfRangeException(
                            nameof(source));
                    using var tempData = stream.Allocate1D<{{valueTypeName}}>(
                        config.GridSize);
                    var rightBoundaries = tempData.View;
                    stream.Launch(config, index => {{firstPassName}}(
                        source, rightBoundaries, identity, numIterationsPerGroup));
                    stream.Launch(config, index => {{secondPassName}}(
                        source, target, rightBoundaries, identity,
                        numIterationsPerGroup));
                }
                """);
            if (firstPass is null || secondPass is null || host is null)
                return null;

            _generated.Peek().Add(firstPass);
            _generated.Peek().Add(secondPass);
            _generated.Peek().Add(host);
            return SyntaxFactory.ParseExpression(
                $"{methodName}({member.Expression}, {source}, " +
                $"{target}, {identity})").WithTriviaFrom(node);
        }

        private ExpressionSyntax? ExpandReduction(
            InvocationExpressionSyntax node,
            MemberAccessExpressionSyntax member,
            IMethodSymbol method)
        {
            // Only inline noncapturing expression operations. Passing delegates
            // through a kernel argument would change the device call graph.
            var hasOutput = method.ReturnsVoid;
            if (node.ArgumentList.Arguments.Count != (hasOutput ? 5 : 4))
                return null;

            var arguments = node.ArgumentList.Arguments;
            var input = arguments[0].Expression;
            var output = hasOutput ? arguments[1].Expression : null;
            var identity = arguments[hasOutput ? 2 : 1].Expression;
            if (arguments[hasOutput ? 3 : 2].Expression is not
                ParenthesizedLambdaExpressionSyntax
                { ParameterList.Parameters.Count: 2,
                  Body: ExpressionSyntax applyBody } apply ||
                arguments[hasOutput ? 4 : 3].Expression is not
                ParenthesizedLambdaExpressionSyntax
                { ParameterList.Parameters.Count: 2,
                  Body: ExpressionSyntax atomicBody } atomic ||
                HasCaptures(applyBody,
                    apply.ParameterList.Parameters[0].Identifier.Text,
                    apply.ParameterList.Parameters[1].Identifier.Text) ||
                HasCaptures(atomicBody,
                    atomic.ParameterList.Parameters[0].Identifier.Text,
                    atomic.ParameterList.Parameters[1].Identifier.Text))
            {
                return null;
            }

            var inputType = model.GetTypeInfo(input).Type;
            if (inputType is null)
                return null;

            var inputTypeName = inputType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var valueType = hasOutput
                ? method.TypeArguments[0]
                : method.ReturnType;
            var valueTypeName = valueType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var outputTypeName = $"global::ILGPU.ArrayView<{valueTypeName}>";
            var methodName = GetMethodName("Reduce", node);
            var initializeName = methodName + "_Initialize";
            var kernelName = methodName + "_Kernel";

            var applyExpression = new ParameterRenamer(
                apply.ParameterList.Parameters[0].Identifier.Text,
                SyntaxFactory.IdentifierName("value"))
                .Visit(applyBody)!;
            applyExpression = new ParameterRenamer(
                apply.ParameterList.Parameters[1].Identifier.Text,
                SyntaxFactory.ParseExpression("input[globalIndex]"))
                .Visit(applyExpression)!;
            var atomicExpression = new ParameterRenamer(
                atomic.ParameterList.Parameters[0].Identifier.Text,
                SyntaxFactory.ParseExpression("output[0]"))
                .Visit(atomicBody)!;
            atomicExpression = new ParameterRenamer(
                atomic.ParameterList.Parameters[1].Identifier.Text,
                SyntaxFactory.IdentifierName("value"))
                .Visit(atomicExpression)!;

            var initializer = SyntaxFactory.ParseMemberDeclaration(
                $"private static void {initializeName}(long index, " +
                $"{outputTypeName} output, {valueTypeName} identity) " +
                "{ output[index] = identity; }");
            var kernel = SyntaxFactory.ParseMemberDeclaration(
                $"private static void {kernelName}({inputTypeName} input, " +
                $"{outputTypeName} output, {valueTypeName} identity) " +
                "{ var value = identity; " +
                "for (long globalIndex = global::ILGPU.Grid.GlobalThreadIndex; " +
                "globalIndex < input.Length; " +
                "globalIndex += global::ILGPU.Grid.GridStrideLoopStride) " +
                $"value = ({applyExpression}); {atomicExpression}; }}");
            var hostHeader = hasOutput
                ? $"private static void {methodName}(" +
                  "global::ILGPU.Runtime.AcceleratorStream stream, " +
                  $"{inputTypeName} input, {outputTypeName} output, " +
                  $"{valueTypeName} identity) " +
                  "{ if (input.Length < 1) " +
                  "throw new global::System.ArgumentOutOfRangeException(nameof(input)); " +
                  "if (output.Length < 1) " +
                  "throw new global::System.ArgumentOutOfRangeException(nameof(output)); " +
                  "var outputView = output.SubView(0, 1);\n"
                : $"private static {valueTypeName} {methodName}(" +
                  "global::ILGPU.Runtime.AcceleratorStream stream, " +
                  $"{inputTypeName} input, {valueTypeName} identity) " +
                  "{ if (input.Length < 1) " +
                  "throw new global::System.ArgumentOutOfRangeException(nameof(input)); " +
                  "using var output = stream.Allocate1D<" +
                  $"{valueTypeName}>(1); var outputView = output.View.BaseView;\n";
            var hostTail = hasOutput
                ? "}"
                : $"{valueTypeName} result = default; " +
                  "outputView.CopyToCPU(stream, ref result, 1); " +
                  "return result; }";
            var host = SyntaxFactory.ParseMemberDeclaration(
                hostHeader +
                "stream.Launch(outputView.Extent, index => " +
                $"{initializeName}(index, outputView, identity));\n" +
                "var config = stream.ComputeGridStrideKernelConfig(" +
                "input.Length, out _);\n" +
                "stream.Launch(config, index => " +
                $"{kernelName}(input, outputView, identity));\n" +
                hostTail);
            if (initializer is null || kernel is null || host is null)
                return null;

            _generated.Peek().Add(initializer);
            _generated.Peek().Add(kernel);
            _generated.Peek().Add(host);
            return SyntaxFactory.ParseExpression(
                hasOutput
                    ? $"{methodName}({member.Expression}, {input}, " +
                      $"{output}, {identity})"
                    : $"{methodName}({member.Expression}, {input}, {identity})")
                .WithTriviaFrom(node);
        }

        private ExpressionSyntax? ExpandSequence(
            InvocationExpressionSyntax node,
            MemberAccessExpressionSyntax member,
            IMethodSymbol method)
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count < 2 || args[1].Expression is not
                SimpleLambdaExpressionSyntax
                { Body: ExpressionSyntax body } lambda ||
                HasCaptures(body, lambda.Parameter.Identifier.Text))
            {
                return null;
            }

            var target = args[0].Expression;
            if (!IsStableView(target))
                return null;
            var targetType = model.GetTypeInfo(target).Type;
            if (targetType is null)
                return null;

            string sequenceLength = "long.MaxValue";
            string batchLength = "1";
            for (int i = 2; i < args.Count; i++)
            {
                var name = args[i].NameColon?.Name.Identifier.Text ??
                    method.Parameters[i + 1].Name;
                if (name == "sequenceLength")
                {
                    if (!IsPositiveConstant(args[i].Expression))
                        return null;
                    sequenceLength = args[i].Expression.ToString();
                }
                else if (name == "sequenceBatchLength")
                {
                    if (!IsPositiveConstant(args[i].Expression))
                        return null;
                    batchLength = args[i].Expression.ToString();
                }
            }

            var value = new ParameterRenamer(
                lambda.Parameter.Identifier.Text,
                SyntaxFactory.IdentifierName("sequenceIndex")).Visit(body);
            var methodName = GetMethodName("Sequence", node);
            var targetTypeName = targetType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var generatedMethod = SyntaxFactory.ParseMemberDeclaration(
                $"private static void {methodName}(long __ilgpuIndex, " +
                $"{targetTypeName} target, long sequenceLength, " +
                "long sequenceBatchLength) { " +
                "long sequenceIndex = __ilgpuIndex / sequenceBatchLength % " +
                $"sequenceLength; target[__ilgpuIndex] = ({value}); }}");
            if (generatedMethod is null)
                return null;
            _generated.Peek().Add(generatedMethod);
            var replacement = SyntaxFactory.ParseExpression(
                $"{member.Expression}.Launch(({target}).Extent, " +
                $"__ilgpuIndex => {methodName}(__ilgpuIndex, {target}, " +
                $"{sequenceLength}, {batchLength}))");
            return replacement.WithTriviaFrom(node);
        }

        private ExpressionSyntax? ExpandTransform(
            InvocationExpressionSyntax node,
            MemberAccessExpressionSyntax member)
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count != 3 || args[2].Expression is not
                SimpleLambdaExpressionSyntax
                { Body: ExpressionSyntax body } lambda ||
                HasCaptures(body, lambda.Parameter.Identifier.Text))
            {
                return null;
            }

            var source = args[0].Expression;
            var target = args[1].Expression;
            if (!IsStableView(source) || !IsStableView(target))
                return null;
            var sourceType = model.GetTypeInfo(source).Type;
            var targetType = model.GetTypeInfo(target).Type;
            if (sourceType is null || targetType is null)
                return null;

            var value = new ParameterRenamer(
                lambda.Parameter.Identifier.Text,
                SyntaxFactory.ParseExpression("source[__ilgpuIndex]"))
                .Visit(body);
            var methodName = GetMethodName("Transform", node);
            var sourceTypeName = sourceType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var targetTypeName = targetType.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);
            var generatedMethod = SyntaxFactory.ParseMemberDeclaration(
                $"private static void {methodName}(long __ilgpuIndex, " +
                $"{sourceTypeName} source, {targetTypeName} target) " +
                $"{{ target[__ilgpuIndex] = ({value}); }}");
            if (generatedMethod is null)
                return null;
            _generated.Peek().Add(generatedMethod);
            var replacement = SyntaxFactory.ParseExpression(
                $"{member.Expression}.Launch(({target}).Extent, " +
                $"__ilgpuIndex => {methodName}(__ilgpuIndex, " +
                $"{source}, {target}))");
            return replacement.WithTriviaFrom(node);
        }

        private bool HasCaptures(
            ExpressionSyntax body,
            params string[] parameterNames)
        {
            var dataFlow = model.AnalyzeDataFlow(body);
            return dataFlow?.Succeeded != true || dataFlow.DataFlowsIn.Any(
                symbol => !parameterNames.Contains(symbol.Name));
        }

        private bool IsPositiveConstant(ExpressionSyntax expression)
        {
            var constant = model.GetConstantValue(expression);
            return constant.HasValue && constant.Value is not null &&
                System.Convert.ToInt64(constant.Value) > 0;
        }

        private static bool IsStableView(ExpressionSyntax expression) =>
            expression is IdentifierNameSyntax or
                MemberAccessExpressionSyntax
                { Expression: IdentifierNameSyntax };

        private static string GetMethodName(
            string algorithm,
            InvocationExpressionSyntax invocation)
        {
            var position = invocation.GetLocation().GetLineSpan()
                .StartLinePosition;
            return $"__ILGPU_{algorithm}_L{position.Line}_C{position.Character}";
        }
    }

    private sealed class ParameterRenamer(
        string oldName,
        ExpressionSyntax replacement) : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node) =>
            node.Identifier.Text == oldName
                ? replacement.WithTriviaFrom(node)
                : base.VisitIdentifierName(node);
    }
}
