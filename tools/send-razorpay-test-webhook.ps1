# Sends a signed, Razorpay-style payment_link.paid webhook, to test signature checking and
# idempotency without a real payment. Use TEST secrets only.
param(
    [Parameter(Mandatory)] [string] $PaymentLinkId,   # plink_... from POST /api/bookings/{id}/payment-link
    [Parameter(Mandatory)] [long]   $AmountPaise,     # booking amount x 100, e.g. 150000 for Rs 1,500
    [Parameter(Mandatory)] [string] $Secret,          # the webhook secret the API uses
    [string] $EventId = "evt_test_$(Get-Random)",     # same id twice = a Razorpay retry
    [string] $Url = "https://localhost:7086/webhooks/razorpay",
    [switch] $Tamper                                  # change the body AFTER signing: must be rejected
)

$now  = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$body = '{"entity":"event","account_id":"acc_test","event":"payment_link.paid","contains":["payment_link","payment"],' +
        '"payload":{"payment_link":{"entity":{"id":"' + $PaymentLinkId + '","amount":' + $AmountPaise +
        ',"amount_paid":' + $AmountPaise + ',"status":"paid"}},' +
        '"payment":{"entity":{"id":"pay_test_' + (Get-Random) + '","amount":' + $AmountPaise +
        ',"status":"captured","method":"upi"}}},"created_at":' + $now + '}'

$bytes = [Text.Encoding]::UTF8.GetBytes($body)
$hmac  = New-Object System.Security.Cryptography.HMACSHA256 (,[Text.Encoding]::UTF8.GetBytes($Secret))
$signature = -join ($hmac.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") })

if ($Tamper) { $bytes = [Text.Encoding]::UTF8.GetBytes($body.Replace('"amount_paid":' + $AmountPaise, '"amount_paid":100')) }

try {
    $r = Invoke-WebRequest -Uri $Url -Method Post -Body $bytes -ContentType "application/json" -UseBasicParsing `
         -Headers @{ "X-Razorpay-Signature" = $signature; "X-Razorpay-Event-Id" = $EventId }
    "$($r.StatusCode) OK   event $EventId"
} catch {
    "$($_.Exception.Response.StatusCode.value__) FAILED   event $EventId   $($_.ErrorDetails.Message)"
}