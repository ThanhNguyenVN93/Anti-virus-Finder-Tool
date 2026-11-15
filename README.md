🛡️ Antivirus Finder Tool

A lightweight Windows utility that detects the currently installed third-party antivirus, downloads its **official** removal tool, verifies its digital signature, and runs it for you — including finishing the removal in Safe Mode when the vendor requires it (e.g. ESET).

✨ Key Features

🔍 **Multi-source Detection** — identifies the installed third-party antivirus using three sources combined: Windows Security Center (`ROOT\SecurityCenter2`), the registry uninstall keys (32/64-bit), and running services/drivers. This also catches half-removed installs and works on Windows Server, where Security Center is not present. Stale/leftover entries are filtered out and product paths with environment variables are normalized so a real AV isn't mistaken for a leftover.
🌐 **Official Uninstaller Download** — resolves the detected AV to its official removal tool (Kaspersky, Avast, AVG, Avira, Bitdefender, ESET, McAfee, Norton, Trend Micro, Malwarebytes, F-Secure/WithSecure, Sophos) and downloads it with live progress/speed. Downloads go to a temporary `.part` file and are only promoted once complete.
🔐 **Signature Verification** — before running anything with Administrator rights, the downloaded tool's Authenticode signature is verified (and the signer is matched against the expected vendor). Unsigned or mismatched files are refused.
🧰 **Safe Mode Removal (ESET)** — some vendors only allow a clean uninstall from Safe Mode. The tool can reboot into Safe Mode Minimal, automatically relaunch itself there, run the uninstaller, then revert the boot configuration and restart back to Normal Mode. A **10-second countdown with a Cancel button** guards every reboot, and the Safe Mode boot flag is always cleared on any error so the machine can never get stuck in Safe Mode.
♻️ **Restore Point + Post-removal Check** — creates a System Restore point before removing an AV, and after removal verifies the AV is really gone and offers to re-enable Windows Defender real-time protection if it's still off.
🧷 **Single Instance** — a global mutex prevents running two or three copies at once (which previously caused duplicate downloads and repeated reboots).
🔁 **No surprise re-downloads** — the tool remembers what it already handled. Re-opening it won't silently re-download or re-run a remover; an already-verified download is offered for reuse instead of being fetched again.
🌍 **Online-updatable catalog** — removal links, expected signers and Safe Mode flags are read from [`avlist.json`](avlist.json) at startup (falling back to the built-in list), so dead links can be fixed without rebuilding the app.
⬇️ **Hardened downloader** — HttpClient-based with retry + resume, cancel button, HTTPS-only, per-hop host pinning (redirects to untrusted hosts are refused) and a Content-Type check that rejects error pages masquerading as downloads.
🧹 **Remnant cleanup** — after removal, previews and (with confirmation) deletes leftover vendor folders and orphaned stopped services.
🧯 **Safe Mode watchdog** — before rebooting into Safe Mode, schedules a failsafe task that clears the boot flag and reboots if the app ever fails to clean up (best-effort; see notes).
🔋 **Preflight checks** — warns on low battery before a Safe Mode reboot, if another installer (MSI) is running, or if Defender Tamper Protection is still on.
🧩 **Multi-AV & productState** — detects multiple third-party AVs and handles them across runs; decodes SecurityCenter2 productState (enabled / up-to-date).
🏢 **Policy for deployments** — an optional `policy.json` (next to the exe or in `%ProgramData%\AVFinder`) can restrict which AVs may be removed, require a restore point, and disable auto-run or auto-reboot.
🌐 **Bilingual UI** — VI/EN toggle for the main interface.
⬆️ **Update check** — checks GitHub Releases for a newer version on launch.
🖥️ **CLI mode** — `--scan-only`, `--json`, `--report` (text + HTML), `--silent [--remove X] [--no-reboot]`, `--help` for technicians and remote management (scan exit code 0 = clean, 1 = third-party AV present). Reports and logs are written to `%ProgramData%\AVFinder`.

🚀 Usage

1. Run the app as Administrator (required for Safe Mode boot configuration via `bcdedit`).
2. It scans automatically on launch and shows the detected antivirus.
3. Confirm the prompt to download + run the official uninstaller.
4. For AVs that require Safe Mode, follow the on-screen steps — the machine will restart (after a countdown you can cancel) and the tool continues automatically.

Command line:

```bash
"Anti-Virus Finder Tool.exe" --scan-only --json
"Anti-Virus Finder Tool.exe" --report
"Anti-Virus Finder Tool.exe" --silent --remove ESET --no-reboot
```

🏢 `policy.json` (optional, for deployments)

```json
{
  "allowAutoReboot": true,
  "requireRestorePoint": true,
  "disableAutoRun": false,
  "allowedAv": ["ESET", "Kaspersky"]
}
```

📦 Releases & verification

Pushing a `v*` tag triggers [`.github/workflows/release.yml`](.github/workflows/release.yml), which builds Release on CI, publishes the exe + a zip, a `SHA256SUMS.txt`, and a **build provenance attestation**. Until signed binaries are available (e.g. via the SignPath Foundation OSS program), verify a download by checking its SHA256 against `SHA256SUMS.txt` and the attestation with `gh attestation verify <file> --repo ThanhNguyenVN93/Anti-virus-Finder-Tool`.

⚠️ Notes

- Requires Windows with `bcdedit`/`shutdown` available (Administrator privileges). `--scan-only`/`--report` still require elevation because the app manifest requests it.
- The Safe Mode flow modifies boot configuration temporarily and reverts it automatically; it is also cleared on cancel or any error.
- The watchdog is a secondary net: Task Scheduler generally does not run in **Safe Mode Minimal**, so the primary guarantee against getting stuck is the app's own clear-on-error/clear-on-close logic. The watchdog helps when the machine reaches a state where the scheduler runs.
- Data directory: `%ProgramData%\AVFinder` (downloads, `state.json`, logs, reports).

📝 `avlist.json` format

```json
[
  { "match": ["eset"], "name": "ESET",
    "url": "https://download.eset.com/.../uninstaller.exe",
    "signer": ["eset"], "safeMode": true, "fileName": "uninstaller.exe" }
]
```

🤝 Contributing

Contributions are welcome! If you have suggestions for improving detection logic or adding compatibility for new security suites, please open an issue or submit a pull request.
