## How to run the application?
### Setup the database
- Create postgreSql database (for example docker container)
- Run all the required scrips against the database to set up tables used by Orleans Clustering/Persistence.
- Make sure that connection strings in Program.cs are valid based on your database deployment

### Ensure unique ports of each silo
If you want to run multiple silo applications make sure to change ports in Program.cs of `siloBuilder.UseLocalClustering(...)`.
You should also change the swagger port in the app config or completely remove Controllers if you do not intend to use them.

### Start applications
Run as many silos as you want to cooperate with each other.

### Running sonarqube
`docker compose -f sonarqube.yml up `

- Open sonarqube on localhost:9000 and login using login=`admin`, password=`admin`
- Change password to `admin123`
- Create a project: `Add a project > manual`. Use `orleans-ticket` in both form inputs
- Create token for `orleans-ticket` and copy it
- Click continue
- Select `.NET` and `.NET Core`
- Install scanner running `dotnet tool install --global dotnet-sonarscanner`
- Execute scan running the following commands:
  - `dotnet sonarscanner begin /k:"orleans-ticket" /d:sonar.host.url="http://localhost:9000"  /d:sonar.login="SAVED_TOKEN"`
  - `dotnet build`
  - `dotnet sonarscanner end /d:sonar.login="SAVED_TOKEN"`