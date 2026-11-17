<#
-------------------------
Helpful terminal commands
-------------------------
To quickly execute some command: in vscode use "Terminal: Run Selected Text In Active Terminal"
(@command:workbench.action.terminal.runSelectedText)
#>

return 0; # prevents executing this script as a whole

Clear-Host
git init


# -----------------------------------------------
# BUILD
# -----------------------------------------------
dotnet clean

dotnet build

dotnet clean; dotnet build --no-incremental

# -----------------------------------------------
dotnet run --project .\AbySalto.Mid\AbySalto.Mid.WebApi.csproj

start http://localhost:5269/index.html # see Swagger definitions

# -----------------------------------------------
# TUnit testing framework
# -----------------------------------------------
dotnet new --help

# install TUnit testing framework templates
dotnet new install TUnit.Templates

# list templates
dotnet new list TUnit

# add new testing project using TUnit template
dotnet new TUnit --name TempTests

# -----------------------------------------------
# TESTING
# -----------------------------------------------
# run all tests
dotnet test

# run only unit tests
dotnet run  --project AbySalto.Mid.Tests -- --treenode-filter "/*/AbySalto.Mid.Tests.Unit/*/*"
dotnet test --project AbySalto.Mid.Tests -- --treenode-filter "/*/AbySalto.Mid.Tests.Unit/*/*"
dotnet test --treenode-filter "/*/AbySalto.Mid.Tests.Unit/*/*"

# -----------------------------------------------
# EF Migrations
# -----------------------------------------------
dotnet ef --help
dotnet ef migrations --help
dotnet ef migrations add --help

# add migration
dotnet ef migrations add InitialCreate  --project AbySalto.Mid.Infrastructure --startup-project AbySalto.Mid

# remove migration
dotnet ef migrations remove             --project AbySalto.Mid.Infrastructure --startup-project AbySalto.Mid

# verbose log of migration
dotnet ef migrations add Initial  --project AbySalto.Mid.Infrastructure --startup-project AbySalto.Mid --verbose

 # Unhandled exception. System.IO.FileNotFoundException: Could not load file or assembly 'System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.

dotnet ef --version
dotnet tool list --global
dotnet tool list --local

# globally installed dotnet-ef tool is 11.0.0
# but the project needs version 9

# create dotnet-tools.json local manifest file
dotnet new tool-manifest

# install correct version of EF locally
dotnet tool install dotnet-ef --version 9.0.23

# -----------------------------------------------
# EF Database update
# -----------------------------------------------
dotnet ef database --help

dotnet ef database drop     --project AbySalto.Mid.Infrastructure --startup-project AbySalto.Mid -- --environment Development

dotnet ef database update   --project AbySalto.Mid.Infrastructure --startup-project AbySalto.Mid -- --environment Development
