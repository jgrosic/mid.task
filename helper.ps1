return 0; # prevents executing this script as a whole

# helper script for running terminal commands
# to quickly execute some command: in vscode use "Terminal: Run Selected Text In Active Terminal" (command id is @command:workbench.action.terminal.runSelectedText)

Clear-Host
git init

dotnet clean

dotnet build

dotnet run --project .\AbySalto.Mid\AbySalto.Mid.WebApi.csproj

start http://localhost:5269/index.html # see Swagger definitions