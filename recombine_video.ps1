Write-Host "Recombining Demo.mp4 from split parts..." -ForegroundColor Green
$combStream = [System.IO.File]::OpenWrite("Demo.mp4")
foreach ($part in @("Demo.mp4.part1", "Demo.mp4.part2")) {
    $bytes = [System.IO.File]::ReadAllBytes($part)
    $combStream.Write($bytes, 0, $bytes.Length)
}
$combStream.Close()
Write-Host "Success: Demo.mp4 has been successfully reconstructed!" -ForegroundColor Green
