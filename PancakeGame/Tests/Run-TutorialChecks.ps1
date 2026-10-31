$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$source = Get-Content (Join-Path $root 'Assets/Scripts/system_tutorial.cs') -Raw
$offset = $source.IndexOf('public sealed class TutorialProgress')
if ($offset -lt 0) { throw 'TutorialProgress source not found' }
# Compile the production progress rules, not a separate implementation of them.
Add-Type -TypeDefinition $source.Substring($offset)
function Check([bool] $condition, [string] $message) {
    if (-not $condition) { throw $message }
}
$p = [TutorialProgress]::new()
Check ($p.Jumps -eq 0) 'Initial state counted a jump'
$p.RecordJump(); $p.RecordJump()
Check ($p.Jumps -lt 3) 'Jump lesson completed after only two successes'
$p.RecordJump()
Check ($p.Jumps -eq 3) 'Third jump was not counted'
$p.Reset()
foreach ($lane in @(0,1,2,0,1)) { $p.RecordLane($lane) }
Check ($p.Lanes[2] -eq 1) 'Lane lesson must require two arrivals in each lane'
$p.RecordLane(2); $p.RecordLane(-1); $p.RecordLane(3)
Check (($p.Lanes | Where-Object { $_ -ne 2 }).Count -eq 0) 'Lane counts incorrect'
$p.Reset()
$p.RecordGoodItem($true, $false); $p.RecordGoodItem($false, $true)
Check ($p.GroundItems -eq 0 -and $p.AirItems -eq 0) 'Wrong pickup height counted'
1..2 | ForEach-Object { $p.RecordGoodItem($false, $false); $p.RecordGoodItem($true, $true) }
Check ($p.GroundItems -eq 2 -and $p.AirItems -eq 2) 'Ground/air quotas incorrect'
$p.RecordBadItem($false); $p.RecordBadItem($false); $p.RecordBadItem($true)
Check ($p.ConsecutiveAvoids -eq 0) 'Hit did not reset avoidance streak'
1..3 | ForEach-Object { $p.RecordBadItem($false) }
Check ($p.ConsecutiveAvoids -eq 3) 'Three consecutive misses not counted'
$t = [TutorialProgress]::AdvanceSafeTime(9, 1, $true, $false)
Check ($t -eq 10) 'Survival timer did not reach ten seconds'
Check ([TutorialProgress]::AdvanceSafeTime(9, 1, $true, $true) -eq 0) 'Failure did not reset timer'
Check ([TutorialProgress]::AdvanceSafeTime(9, 1, $false, $false) -eq 0) 'Inactive fire counted toward avoidance'
$p.Reset()
Check ($p.Jumps -eq 0 -and $p.GroundItems -eq 0 -and $p.AirItems -eq 0 -and $p.ConsecutiveAvoids -eq 0) 'Lesson reset leaked progress'
$contact = [TutorialProgress]::AdvanceContactTime(0, 2, $true)
Check ($contact -eq 2) 'Fire contact incorrectly completed before three seconds'
$contact = [TutorialProgress]::AdvanceContactTime($contact, 5, $false)
Check ($contact -eq 2) 'Refill or time away counted toward fire contact'
$contact = [TutorialProgress]::AdvanceContactTime($contact, 1, $true)
Check ($contact -eq 3) 'Fire contact did not resume after refill'
Check ([TutorialProgress]::AdvanceContactTime(2, -1, $true) -eq 2) 'Negative time reduced fire contact'
Write-Output 'Tutorial progress checks: 17 assertions passed.'
