dotnet restore OTAPI.Mods.sln
dotnet restore OTAPI.Server.Launcher.sln
dotnet build OTAPI.Mods.sln
cd OTAPI.Patcher/bin/Debug/net5.0
dotnet run --project ../../../OTAPI.Patcher.csproj -patchTarget=p -latest=n --framework net5.0
cd ../../../../
dotnet build OTAPI.Server.Launcher.sln
cd OTAPI.Server.Launcher/bin/Debug/net5.0
dotnet OTAPI.Server.Launcher.dll -test-init
cd ../../../../
cd OTAPI.Patcher/bin/Debug/net5.0
dotnet run --project ../../../OTAPI.Patcher.csproj -patchTarget=m -latest=n --framework net5.0
cd ../../../../
dotnet build OTAPI.Server.Launcher.sln
cd OTAPI.Server.Launcher/bin/Debug/net5.0
dotnet OTAPI.Server.Launcher.dll -test-init
cd ../../../../
cd OTAPI.Patcher/bin/Debug/net5.0
dotnet run --project ../../../OTAPI.Patcher.csproj -patchTarget=t -latest=n --framework net5.0
cd ../../../../
dotnet build OTAPI.Server.Launcher.sln
cd OTAPI.Server.Launcher/bin/Debug/net5.0
dotnet OTAPI.Server.Launcher.dll -test-init
cd ../../../../