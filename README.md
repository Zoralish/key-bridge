# KeyBridge
 
KeyBridge is a Windows command-line utility that simplifies two manual KeePass workflows:

- Opening a local KeePass database.
- Synchronizing local database with a cloud-stored copy.

The project acts as a bridge between KeePass, KPScript, local storage, and cloud-synchronized files while keeping the workflow explicit and user-controlled.
 
**Stack:** C# · .NET 10 · Spectre.Console · ASP.NET Core Data Protection Extensions · Native AOT
 
## Highlights
 
- Open and sync for a local KeePass database and a cloud-mirrored copy (OneDrive, iCloud, etc.)
- Files are fully hydrated before KeePass or KPScript touch them
- Master password encrypted at rest via Windows DPAPI, decrypted only in memory and piped to child processes over `stdin`
- Rotating local backups before every sync
- Trimmed, self-contained, Native AOT single-file `.exe`
## Requirements
 
- Windows 11
- KeePass 2.x with the KPScript plugin
 
## Usage
 
First run walks through setup: paths to KeePass, KPScript, both databases, and your key file (all droppable directly onto the console window), plus your master password. After that:
 
- **Open local database** — decrypts, hydrates, launches KeePass with the password piped in, then exits immediately; KeePass's own window is the confirmation.
- **Synchronize databases** — hydrates both copies, backs them up, runs KPScript's sync, and reports the result.
- **Hard reset** — wipes configuration, keys, and backups after a confirmation prompt.
## Design tradeoffs
 
- Manually operated by design, with no automation. The tool exists to make that manual step faster, not to remove it.
- Built on existing tools rather than reimplementing them: KeePass and KPScript own the database and sync logic, Windows DPAPI owns encryption, the cloud client owns file sync — KeyBridge just coordinates all of it.
- One local database, one cloud database, one key file per config — single-profile by design.

## Building from source
 
```
dotnet publish -c Release -r win-x64
```
 
Produces a single, self-contained `KeyBridge.exe`. It's Native AOT, so building it also requires the C++ build tools workload in Visual Studio.
