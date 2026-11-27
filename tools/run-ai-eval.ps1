# Runs the hand-labelled evaluation set through POST /api/ai/triage-preview (nothing is saved)
# and scores priority and service against the expected labels.
# The API key is read from $env:SERVICEDESK_API_KEY so it never appears in your command history.
param(
    [string] $BaseUrl = "https://localhost:7086",
    [string] $ApiKey = $env:SERVICEDESK_API_KEY,
    [string] $EvalFile = "ai/eval/eval-set.json",
    [int] $DelaySeconds = 5                          # stay under the free tier's requests-per-minute limit
)

$cases = Get-Content $EvalFile -Raw -Encoding UTF8 | ConvertFrom-Json

$rows = foreach ($c in $cases) {
    $body = [Text.Encoding]::UTF8.GetBytes((@{ message = $c.message } | ConvertTo-Json))
    try {
        $r = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/ai/triage-preview" `
             -Headers @{ "X-Api-Key" = $ApiKey } -ContentType "application/json; charset=utf-8" -Body $body
    } catch {
        $r = [pscustomobject]@{ status = "HTTP $($_.Exception.Response.StatusCode.value__)"; promptVersion = "?" }
    }
    [pscustomobject]@{
        Id         = $c.id
        Expected   = $c.expectedPriority
        Got        = $r.priority
        PriorityOk = ($r.priority -eq $c.expectedPriority)
        ExpService = $c.expectedService
        GotService = $r.service
        ServiceOk  = ($r.service -eq $c.expectedService)
        Status     = $r.status
        Masked     = "$($r.emailsMasked)/$($r.phonesMasked)"
        Version    = $r.promptVersion
    }
    Start-Sleep -Seconds $DelaySeconds
}

$n = @($rows).Count
$p = @($rows | Where-Object { $_.PriorityOk }).Count
$s = @($rows | Where-Object { $_.ServiceOk }).Count
$version = @($rows)[0].Version

$rows | Format-Table Id, Expected, Got, PriorityOk, GotService, ServiceOk, Status, Masked -AutoSize
"Prompt $version   Priority: $p/$n correct   Service: $s/$n correct"

# Save a markdown report next to the evaluation set
$report = @(
    "# Evaluation, prompt $version, $(Get-Date -Format 'yyyy-MM-dd HH:mm')",
    "",
    "**Priority: $p/$n correct | Service: $s/$n correct**",
    "",
    "| Id | Expected | Got | Priority ok | Expected service | Got service | Service ok | Status | Masked (emails/phones) |",
    "|---|---|---|---|---|---|---|---|---|"
)
$report += $rows | ForEach-Object {
    "| $($_.Id) | $($_.Expected) | $($_.Got) | $($_.PriorityOk) | $($_.ExpService) | $($_.GotService) | $($_.ServiceOk) | $($_.Status) | $($_.Masked) |"
}
$report | Set-Content -Encoding UTF8 "ai/eval/results-$version.md"
"Report saved to ai/eval/results-$version.md"
