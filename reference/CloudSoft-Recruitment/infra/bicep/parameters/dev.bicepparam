using '../main.bicep'

param environment = 'dev'
param location = 'norwayeast'
param containerImage = 'mcr.microsoft.com/dotnet/samples:aspnetapp'
param adminSeedPassword = readEnvironmentVariable('ADMIN_SEED_PASSWORD', 'Admin123!')
param candidateSeedPassword = readEnvironmentVariable('CANDIDATE_SEED_PASSWORD', 'Candidate123!')
param jwtKey = readEnvironmentVariable('JWT_KEY', '')
