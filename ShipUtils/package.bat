cd "../../../Package Files"
del package.zip
powershell -command "$json = Get-Content .\manifest.json -raw | ConvertFrom-Json;$json.version_number=([Xml](Get-Content ..\ShipUtils.csproj)).Project.PropertyGroup.Version;$json | ConvertTo-Json | Set-Content .\manifest.json"
tar -a -c -f package.zip icon.png README.md manifest.json