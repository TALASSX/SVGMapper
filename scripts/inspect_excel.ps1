$path = Join-Path (Get-Location) 'OSSRequestForm-v4.xlsx'
$xl = New-Object -ComObject Excel.Application
$xl.Visible = $false
$wb = $xl.Workbooks.Open($path)
$ws = $wb.Worksheets.Item(1)
$ur = $ws.UsedRange
$rows = $ur.Rows.Count
$cols = $ur.Columns.Count
Write-Host "Rows: $rows, Cols: $cols"
for ($r=1; $r -le [Math]::Min(40,$rows); $r++) {
    $line = @()
    for ($c=1; $c -le [Math]::Min(12,$cols); $c++) {
        $val = $ws.Cells.Item($r,$c).Text
        if ($null -eq $val) { $val = '' }
        $line += $val
    }
    Write-Host ($line -join ' | ')
}
$wb.Close($false)
$xl.Quit()
