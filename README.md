# Discord.InviteFilter
A Discord bot that filters invitations to other servers and performs actions configured by the user accordingly.\
Invitations to the current server are allowed.

**Possible actions:**
- Delete the message of the user
- Timeout the user for x minutes
- Delete and timeout the user
- Ban the user
- Delete and ban the user
- *Disabled (Default)*

## Prerequisites (for self hosting)
- Create a Discord Application here: https://discord.com/developers/applications
  - Now create a Bot Account for that application to get the **Bot Token**
- MariaDB or MySQL (not recommended) database

## Setup
### Add the official bot
You can add the bot to your server via this link: https://discord.com/api/oauth2/authorize?client_id=935880541725655140&permissions=1511828876404&scope=bot%20applications.commands

### Self host
Alternatively you can download the latest release from the [Releases tab](https://github.com/TheDusty01/Discord.InviteFilter/releases) and host it yourself.\
Make sure to provide the settings via environment variables or through the [appsettings.json](/Discord.InviteFilter/appsettings.json) file.

You can also run this app in a docker container, a [Dockerfile](/Discord.InviteFilter/Dockerfile) is ready to be used.

**Setup the database:**
1. Install [EF Tools](https://docs.microsoft.com/de-de/ef/core/cli/dotnet): ``dotnet tool install --global dotnet-ef``
2. Navigate to [Discord.InviteFilter](Discord.InviteFilter)
3. Run: ``dotnet ef database update``

## Usage
1. Invite the bot to your server
2. Use the ``/invfilter setup`` command to setup the punishment for sending invitations to other servers
3. Done!

## Build
### Visual Studio
1. Open the solution with Visual Studio 2022
2. Build the solution
3. (Optional) Publish the solution

### .NET CLI
1. ``dotnet restore "Discord.InviteFilter/Discord.InviteFilter.csproj"``
2. ``dotnet build "Discord.InviteFilter/Discord.InviteFilter.csproj" -c Release``
3. (Optional) ``dotnet publish "Discord.InviteFilter/Discord.InviteFilter.csproj" -c Release``

Output directory: ``Discord.InviteFilter\Discord.InviteFilter\bin\Release\net6.0`` \
Publish directory: ``Discord.InviteFilter\Discord.InviteFilter\bin\Release\net6.0\publish``

## Credits
This project uses the following open-source projects:
- https://github.com/DSharpPlus/DSharpPlus

## License
Discord.InviteFilter is licensed under the MIT License, see [LICENSE.txt](/LICENSE.txt) for more information.