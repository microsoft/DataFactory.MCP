Add-Type -AssemblyName PresentationFramework
[xml]$xaml = @"
{{XamlContent}}
"@
$reader = New-Object System.Xml.XmlNodeReader $xaml
$window = [Windows.Markup.XamlReader]::Load($reader)
$window.Left = [System.Windows.SystemParameters]::WorkArea.Right - 350
$window.Top = [System.Windows.SystemParameters]::WorkArea.Bottom - 120
$timer = New-Object System.Windows.Threading.DispatcherTimer
$timer.Interval = [TimeSpan]::FromSeconds(5)
$timer.Add_Tick({ $window.Close() })
$timer.Start()
$window.ShowDialog()
