# Runs the main scenario (MixedUser) at 10, 50, 100 and 200 users, 2 minutes each, and makes
# the table for the thesis. Start from the load-tests folder with the virtual environment active:
#   .\pokreni_nivoe.ps1
#   .\pokreni_nivoe.ps1 -HostUrl https://localhost:7221 -Trajanje 3m
param(
    [string]$HostUrl = "https://localhost:7221",
    [string]$Trajanje = "2m",
    [int[]]$Nivoi = @(10, 50, 100, 200),
    [string]$Scenario = "MixedUser",
    [string]$Naziv = "mix"
)

New-Item -ItemType Directory -Force -Path rezultati | Out-Null

foreach ($korisnika in $Nivoi) {
    Write-Host "`n=== $Scenario, $korisnika korisnika, $Trajanje ===" -ForegroundColor Cyan
    locust -f locustfile.py $Scenario --headless `
        --host $HostUrl `
        -u $korisnika -r 10 -t $Trajanje `
        --csv "rezultati/${Naziv}_$korisnika" `
        --html "rezultati/${Naziv}_$korisnika.html" `
        --only-summary
    # A short pause so the API finishes the last requests before the next level starts.
    Start-Sleep -Seconds 10
}

python sazetak.py "rezultati/$Naziv"
