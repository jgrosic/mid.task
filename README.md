# Abysalto .net mid task

see: helper.ps1 on how to run commands in terminal

Project dependencies, arrow meaning "project is referencing another project"
- Application     -> Domain
- Infrastructure  -> Application + Domain
- WebApi          -> Application + Infrastructure

To quickly add "Domain" as an "Application" dependency, drag and drop Domain to Application in VS Solution Explorer.

Projects
AbySalto.Mid.Domain
AbySalto.Mid.Application
AbySalto.Mid.Infrastructure
AbySalto.Mid — Web API

