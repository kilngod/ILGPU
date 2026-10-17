$names = @(
    'amdhip64',
    'amdhip64_7',
    'C:\Program Files\AMD\ROCm\7.2\bin\amdhip64_7.dll'
)
"Process64=$([Environment]::Is64BitProcess)"
foreach ($name in $names) {
    $handle = [IntPtr]::Zero
    $loaded = [System.Runtime.InteropServices.NativeLibrary]::TryLoad($name, [ref]$handle)
    "$name : $loaded"
    if ($loaded) {
        [System.Runtime.InteropServices.NativeLibrary]::Free($handle)
    }
}
