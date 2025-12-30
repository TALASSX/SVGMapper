$path = Join-Path (Get-Location) 'OSSRequestForm-v4.xlsx'
$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$wb = $xl.Workbooks.Open($path)
$ws = $wb.Worksheets.Item(1)

# mapping of form keys (col C) to values to write into col D
$values = @{
    'name' = 'SVGMapper'
    'handle' = 'svgmapper'
    'type' = 'Program'
    'license' = 'MIT (https://opensource.org/licenses/MIT)'
    'repository_url' = 'https://github.com/TALASSX/SVGMapper'
    'homepage_url' = 'https://github.com/TALASSX/SVGMapper'
    'download_url' = 'https://github.com/TALASSX/SVGMapper/releases'
    'privacy_policy_url' = 'https://github.com/TALASSX/SVGMapper/blob/main/README.md#privacy-policy'
    'wikipedia_url' = ''
    'tagline' = 'Small WPF app for mapping SVG floor plans and seating.'
    'description' = 'SVGMapper is a small WPF application for mapping SVG floor plans and assigning seats, with export features and packaging for Windows.'
    'reputation' = 'GitHub repository: https://github.com/TALASSX/SVGMapper; Releases and CI activity present.'
    'User Full Name' = 'Sai Teja Talasila'
    'User Email' = 'talasilasaiteja1255@mail.com'
    'Build System' = 'GitHub Actions (windows-latest), workflow: .github/workflows/release.yml'
    'terms_accepted' = 'I hereby accept the terms of use'
}

$used = $ws.UsedRange
$rows = $used.Rows.Count
for ($r=1; $r -le $rows; $r++) {
    $key = $ws.Cells.Item($r,3).Text
    if (-not $key) { continue }
    $k = $key.Trim()
    # normalize
    $lk = $k.ToLower()
    if ($values.ContainsKey($lk)) {
        $val = $values[$lk]
        $ws.Cells.Item($r,4).Value2 = $val
        Write-Host "Filled $k -> $val"
    } else {
        # also try exact matches for weird keys
        if ($values.ContainsKey($k)) {
            $val = $values[$k]
            $ws.Cells.Item($r,4).Value2 = $val
            Write-Host "Filled $k -> $val"
        }
    }
}

# Special handling: accept terms cell (search for 'terms_accept' key)
for ($r=1; $r -le $rows; $r++) {
    $key = $ws.Cells.Item($r,3).Text
    if ($key -and $key.Trim().ToLower() -eq 'terms_accept') {
        $ws.Cells.Item($r,4).Value2 = 'I hereby accept the terms of use'
        Write-Host "Filled terms_accept"
    }
}

$wb.Save()
$wb.Close($true)
$xl.Quit()
Write-Host "Form filled and saved: $path"