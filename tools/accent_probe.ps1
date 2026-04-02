# Reads the current Windows accent palette and outputs CSV-friendly data
# Usage: Run this script, change accent in Settings > Personalization > Colors, run again

$key = Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent'
$p = $key.AccentPalette

# Layout: Light3[0:3], Light2[4:7], Light1[8:11], Accent[12:15], Dark1[16:19], Dark2[20:23], Dark3[24:27]
$accentR = $p[12]; $accentG = $p[13]; $accentB = $p[14]
$hex = '#{0:X2}{1:X2}{2:X2}' -f $accentR, $accentG, $accentB

Write-Host "Accent: $hex"
Write-Host ""
Write-Host "variant,R,G,B,hex"

$names = @('Light3','Light2','Light1','Accent','Dark1','Dark2','Dark3')
for ($i = 0; $i -lt 7; $i++) {
    $r = $p[$i*4]; $g = $p[$i*4+1]; $b = $p[$i*4+2]
    $h = '#{0:X2}{1:X2}{2:X2}' -f $r, $g, $b
    Write-Host "$($names[$i]),$r,$g,$b,$h"
}
