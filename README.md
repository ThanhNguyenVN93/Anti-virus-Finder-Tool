🛡️ Antivirus Finder Tool

A lightweight Windows utility that detects the currently installed third-party antivirus, downloads its official removal tool, and runs it for you — including finishing the removal in Safe Mode when the vendor requires it (e.g. ESET).

✨ Key Features

🔍 **Antivirus Detection** — queries Windows Security Center (`ROOT\SecurityCenter2`) to identify the installed third-party antivirus, filtering out stale/leftover entries so a prior AV update doesn't get reported as a second product.
🌐 **Official Uninstaller Download** — resolves the detected AV to its official removal tool (Kaspersky, Avast, AVG, Avira, Bitdefender, ESET, McAfee, Norton, Trend Micro) and downloads it with live progress/speed.
🧰 **Safe Mode Removal (ESET)** — some vendors only allow a clean uninstall from Safe Mode. The tool can reboot the machine into Safe Mode Minimal, automatically relaunch itself there, run the uninstaller, then revert the boot configuration and restart back to Normal Mode.
🖥️ **OS Detection** — reports the Windows version/architecture and flags unprotected legacy systems (e.g. Windows 7 without a modern AV).

🚀 Usage

1. Run the app as Administrator (required for Safe Mode boot configuration via `bcdedit`).
2. It scans automatically on launch and shows the detected antivirus.
3. Click **DOWNLOAD** to fetch the official uninstaller (or let auto-download run).
4. For AVs that require Safe Mode, follow the on-screen steps — the machine will restart and the tool continues automatically.

⚠️ Notes

- Requires Windows with `bcdedit`/`shutdown` available (Administrator privileges).
- The Safe Mode flow modifies boot configuration temporarily and reverts it automatically after the uninstaller finishes.

🤝 Contributing

Contributions are welcome! If you have suggestions for improving detection logic or adding compatibility for new security suites, please open an issue or submit a pull request.
