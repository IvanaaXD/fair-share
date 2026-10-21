# Runs one scenario (MixedUser by default) at 10, 50, 100 and 200 users, 2 minutes each, and makes
# the table for the thesis. Start from the load-tests folder with the virtual environment active:
#   .\run_levels.ps1
#   .\run_levels.ps1 -HostUrl https://localhost:7221 -Duration 3m
#   .\run_levels.ps1 -Levels 400,800 -Duration 4m -Name stress
param(
    [string]$HostUrl = "https://localhost:7221",
    [string]$Duration = "2m",
    [int[]]$Levels = @(10, 50, 100, 200),
    [string]$Scenario = "MixedUser",
    [string]$Name = "mix"
)

New-Item -ItemType Directory -Force -Path results | Out-Null

foreach ($users in $Levels) {
    Write-Host "`n=== $Scenario, $users users, $Duration ===" -ForegroundColor Cyan
    locust -f locustfile.py $Scenario --headless `
        --host $HostUrl `
        -u $users -r 10 -t $Duration `
        --csv "results/${Name}_$users" `
        --html "results/${Name}_$users.html" `
        --only-summary
    # A short pause so the API finishes the last requests before the next level starts.
    Start-Sleep -Seconds 10
}

python summary.py "results/$Name"
