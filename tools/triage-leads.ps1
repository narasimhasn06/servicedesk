# Triages leads one by one through the API and prints a results table.
# The API key is read from $env:SERVICEDESK_API_KEY so it never appears in your command history.
param(
    [Parameter(Mandatory)] [int[]] $LeadIds,
    [string] $BaseUrl = "https://servicedesk-api-tpzr.onrender.com",
    [string] $ApiKey = $env:SERVICEDESK_API_KEY,
    [int] $DelaySeconds = 5                       # stay under the free tier's requests-per-minute limit
)

$results = foreach ($id in $LeadIds) {
    try {
        $r = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/leads/$id/triage" -Headers @{ "X-Api-Key" = $ApiKey }
        [pscustomobject]@{ Lead = $id; Status = $r.status; Priority = $r.priority; Service = $r.service; Summary = $r.summary }
    } catch {
        [pscustomobject]@{ Lead = $id; Status = "HTTP $($_.Exception.Response.StatusCode.value__)"; Priority = ""; Service = ""; Summary = "" }
    }
    Start-Sleep -Seconds $DelaySeconds
}
$results | Format-Table -AutoSize -Wrap