# StarboundPlanner

<p align="center">
   <img width="300" height="300" alt="icon" src=".github/icon.png"/><br><br>
   Starbound Labs' customized ArduPilot ground control station, with a new dark interface and enhanced flight-planning tools.
   <br><br>
   <img width="2560" height="1540" alt="image" src="https://github.com/user-attachments/assets/c352a50b-89bf-42c2-a591-75aa20c94a55" /><br><br>
   <img width="1299" height="1056" alt="image" src="https://github.com/user-attachments/assets/ef922968-cc05-4402-bb51-fa37d8a365df" /><br><br>
   <img width="1278" height="809" alt="image" src="https://github.com/user-attachments/assets/d82c06ba-66a9-4a49-998e-9ab056a240e0" /><br><br>
   <img width="1718" height="1093" alt="image" src="https://github.com/user-attachments/assets/0afb4ca6-5123-49fb-bb2a-0c4430df698f" /><br>
</p>

---

## Enhancements

This fork implements every feature we've always yearned for with considerable UI/UX upgrades, a reworked HUD, a real-time 3D map with vehicle rendering (and ADSB), an overhauled parameter editor and tab layout, a Betaflight-style motor setup, USB auto-connect, offline map packs for air-gapped planning, and dozens of large improvements throughout the app.

**See [CHANGES.md](CHANGES.md) for the full list of features and fixes added in this fork.**

---

## Installation

### Windows (Recommended)

Grab the latest StarboundPlanner installer from [Releases](https://github.com/Titan-Dynamics/TitanPlanner/releases/latest). The app will notify you on updates from then on. Existing TitanPlanner settings and data are migrated on first launch.

### Building from Source

Requires Visual Studio 2022.

```bash
git clone https://github.com/Titan-Dynamics/TitanPlanner.git
cd TitanPlanner
git submodule update --init
```

Open `MissionPlanner.sln` in Visual Studio 2022 and Build.

Start `bin\Release\net461\StarboundPlanner.exe` after a Release build. The executable, toolbar logo, Windows icon, and startup screen use Starbound Labs branding.
The toolbar wordmark stays centered as the window resizes where space allows, without overlapping the navigation or connection controls.

The updater builds from source alongside the app. Release maintainers with the original signing certificate can enable its signing step with `-p:SignUpdater=true`.

### 3D Map

On **Flight Data**, tick **3D Map** beside **Params** above the map to open the terrain viewer below it. The gear button in the viewer opens camera, vehicle scale/color, and custom STL settings. With a connected vehicle, drag to orbit, use the mouse wheel to change camera distance, and double-click to reset the orbit angle.

Quadrotors use the Starbound website flight-log viewer's top-down drone image on the 2D map and its drone geometry in 3D. The embedded STL is converted from that viewer's `public/models/drone.glb`, centered and oriented for the existing millimetre-based renderer. STL preserves geometry, not the website's textures/materials; the 3D vehicle uses the color selected in 3D settings. Other vehicle types and custom STL overrides are unchanged.

### Linux (Mono)

```bash
sudo apt install mono-complete mono-runtime libmono-system-windows-forms4.0-cil \
    libmono-system-core4.0-cil libmono-winforms4.0-cil libmono-corlib4.0-cil \
    libmono-system-management4.0-cil libmono-system-xml-linq4.0-cil

mono StarboundPlanner.exe
```

See [Linux/README.md](Linux/README.md) for the upstream build (`./Linux/build.sh`) and launch (`./Linux/run.sh`) scripts. The prebuilt Linux and macOS downloads that document links to are published by upstream only; this fork ships Windows releases.

---

## Platform Support

| Platform | Status |
|----------|--------|
| Windows | ✅ Full Support |
| Linux (Mono) | ⚠️ WIP |
| macOS | ⚠️ WIP |
| Android | ⚠️ WIP |
| iOS | ⚠️ WIP |

---

## Upstream

- Upstream repository: https://github.com/ArduPilot/MissionPlanner
- ArduPilot website: http://ardupilot.org/planner/
- Forum: http://discuss.ardupilot.org/c/ground-control-software/mission-planner

## License

See [COPYING.txt](COPYING.txt).
