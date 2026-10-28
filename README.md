# AI Code Blocker

A small Windows 11 focus tool: flip one switch and AI assistants are gone until you flip it back.

## What it's for

Practising programming without AI. When a problem gets hard, ChatGPT, Claude or Copilot are always one click away, and the habit of asking them stops you from learning to solve it yourself. AI Code Blocker removes that one click for the length of a practice session, while search engines, documentation, Stack Overflow, GitHub and your compiler/debugger keep working.

It's a **self-discipline tool**, not a security product. You can always switch it off. The point is that turning AI back on becomes a deliberate decision instead of a reflex.

## How it works

```
WPF window (toggle)  ──named pipe──►  Windows service (admin)
                                          ├─ OnlineBlocker  → hosts file
                                          └─ LocalBlocker   → stops Ollama
```

- **Toggle ON**: the service validates `config.json`, then:
  - **OnlineBlocker** adds a marked block to the Windows hosts file that points AI domains (ChatGPT, Claude, Gemini, Copilot, Perplexity, …) at `0.0.0.0`.
  - **LocalBlocker** stops local model servers (Ollama) and keeps stopping them while ON.
- **Toggle OFF**: the service removes only its own block. Nothing else in the hosts file is touched.
- **If anything fails**, everything that already succeeded is undone and the error is shown. The toggle is always either fully ON or fully OFF.
- **Restart-proof**: the service starts with Windows and reapplies ON if it was on before.

## Under the hood

The code is split into small parts, each with one job:

| Part | Job |
|---|---|
| `ConfigLoader` | Reads `config.json` and rejects typos (e.g. `https://chatgpt.com/` instead of `chatgpt.com`) before anything is changed |
| `Orchestrator` | Runs the blockers in order; if one fails, undoes the ones that succeeded (last on, first off) |
| `OnlineBlocker` | Adds/removes the app's own block in the hosts file, between `# >>> AiCodingBlocker` and `# <<< AiCodingBlocker` |
| `LocalBlocker` | Stops Ollama and re-checks every 30 s while ON. No Ollama on the PC? Nothing to stop, it simply succeeds |
| `StateStore` | Saves ON/OFF to `state.json` so the service can reapply after a restart and the UI can show the right state |

What happens when you press **ON**:

1. The UI sends `ON` to the service through a local named pipe (only this PC can connect).
2. `ConfigLoader` validates the config. Any problem → nothing is touched, the error is shown.
3. `Orchestrator` runs `OnlineBlocker` (hosts file, then DNS cache flush), then `LocalBlocker` (stop Ollama, start the guard).
4. If one of them fails, the other is undone, and you're back to OFF with an error message.
5. `StateStore` saves the result, and the service replies to the UI, which updates the toggle.

Pressing **OFF** does the same in reverse: guard stopped, hosts block removed, state saved as OFF. Running ON twice never duplicates anything.

## Why it's built this way

- **Two processes, on purpose.** The UI runs without admin rights; only the small background service has them. The service also keeps the blocks in place when the window is closed or the PC restarts.
- **Reversible and safe.** It backs up the hosts file before the first change and only ever edits its own marked block. It never rewrites the whole file or touches the firewall.
- **Honest status.** The UI shows what was actually applied, never just what you clicked.
- **Configurable.** Domains and processes live in `config.json` and are validated, so a typo fails loudly instead of silently not blocking.
- **Private.** No accounts, no telemetry; nothing about your browsing or code leaves the PC.

## Limits

Domain blocking can't cover every AI feature on the web, and an admin can always switch it off or bypass it. That's fine: the goal is *AI is no longer one click away*, not *AI is impossible*.

## Install

Requires Windows 11 and the .NET 10 SDK.

1. Open **PowerShell as Administrator** (Win + X → *Terminal (Admin)*, the title must start with "Administrator:") and `cd` into the repository folder.
   If scripts are blocked on your PC, use `powershell -ExecutionPolicy Bypass -File .\scripts\install-service.ps1` in step 2.
2. Install and start the background service (one time only):

   ```powershell
   .\scripts\install-service.ps1
   ```

   This publishes the service and the window to `C:\Program Files\AiCodingBlocker`, copies the default blocklist to `C:\ProgramData\AiCodingBlocker\config.json` (only if you don't have one yet), registers the service to start with Windows and creates **AI Code Blocker** shortcuts on the Desktop and in the Start menu.
   Run it again any time to update.

Other PCs without the .NET SDK need the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

## Use

1. Double-click the **AI Code Blocker** shortcut (no admin needed).

2. Click the switch:
   - **Green / ON**: AI sites are blocked and Ollama is stopped. You can close the window; the block stays.
   - **Grey / OFF**: everything the app added is removed.
3. If something goes wrong, a popup tells you what. The switch only changes when the service confirms it, so what you see is what's applied.
4. "Service not running" means the background service isn't installed or was stopped. Check it with `sc.exe query AiCodingBlocker`.

**Changing the blocklist:** edit `C:\ProgramData\AiCodingBlocker\config.json` as admin, then switch OFF and ON again. Domains must be bare hostnames like `chatgpt.com` (no `https://`, no paths). A typo is rejected with a message instead of silently not blocking.

### Required: turn off your browser's Secure DNS

Most browsers use **Secure DNS** (DNS over HTTPS) by default. It looks up websites on its own and skips the Windows hosts file, so the block has no effect in that browser. AI Code Blocker deliberately doesn't change your browser settings, so switch it off once yourself:

| Browser | Where | Then |
|---|---|---|
| Edge | `edge://settings/privacy` → *Security* → **Use secure DNS** → Off | open `edge://restart` |
| Chrome | `chrome://settings/security` → **Use secure DNS** → Off | open `chrome://restart` |
| Brave | `brave://settings/security` → **Use secure DNS** → Off | restart Brave |
| Firefox | *Settings* → *Privacy & Security* → **DNS over HTTPS** → Off | restart Firefox |

**Edge tip:** with *Startup boost* on (`edge://settings/system`), closing Edge doesn't really close it, so old lookups stay cached. Use `edge://restart` or turn Startup boost off.

**Site still loads?** Restart the browser after switching ON. A tab that was open before can keep its old connection for a while.

## Uninstall

In an admin PowerShell in the repository folder:

```powershell
.\scripts\uninstall-service.ps1
```

It turns Focus Mode OFF first, then removes the service, the program folder and the shortcuts. If Focus Mode can't be turned OFF, it stops and tells you why instead of leaving blocks behind (`-Force` skips that check).
Your config and hosts backups in `C:\ProgramData\AiCodingBlocker` are kept; add `-RemoveData` to delete them too.

## Build

```
dotnet build
```
