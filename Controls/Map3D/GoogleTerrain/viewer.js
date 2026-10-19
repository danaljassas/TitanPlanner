import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { DRACOLoader } from "three/addons/loaders/DRACOLoader.js";
import { TilesRenderer } from "3d-tiles-renderer/three";
import { GoogleCloudAuthPlugin } from "3d-tiles-renderer/core/plugins";
import { GLTFExtensionsPlugin, ReorientationPlugin } from "3d-tiles-renderer/three/plugins";

const notice = document.getElementById("notice");
const size = document.getElementById("size");
const orbit = document.getElementById("orbit");
const zoom = document.getElementById("zoom");
const report = message => window.chrome.webview.postMessage(message);
const radians = THREE.MathUtils.degToRad;
const minimumRange = 3, maximumRange = 3000;
let renderer, scene, camera, controls, tiles, reorientation, terrain, drone, headingArrow, key;
let targetRange = 60, currentRange = 60, targetOrbit = 0, currentOrbit = 0;
let lastPosition, currentPosition, targetPosition, targetRotation, currentRotation;
let following = true, lastFrame = 0, lastGroundSample = 0, groundHeight;
let rootLoaded = false, initialized = false, failedTiles = false, modelLoaded = false;
const raycaster = new THREE.Raycaster();
const down = new THREE.Vector3(0, -1, 0);
const targetEuler = new THREE.Euler(0, 0, 0, "YXZ");

function error(text, type = "tiles-error") {
    notice.textContent = text;
    notice.hidden = false;
    report(type);
}
function setRange(range) {
    targetRange = THREE.MathUtils.clamp(range, minimumRange, maximumRange);
    zoom.value = 100 * Math.log(targetRange / minimumRange) / Math.log(maximumRange / minimumRange);
    document.getElementById("zoom-value").textContent = Math.round(targetRange) + " m";
}
function resetView() {
    following = true;
    targetOrbit = 0;
    orbit.value = 0;
    document.getElementById("orbit-value").textContent = "0°";
    setRange(lastPosition && lastPosition.connected ? 60 : 600);
}
function earthPosition(data) {
    // WGS84 ECEF, transformed by the same local frame as Google's tile geometry.
    const lat = radians(data.lat), lon = radians(data.lon);
    const radius = 6378137 / Math.sqrt(1 - 0.00669437999014 * Math.sin(lat) ** 2);
    const altitude = data.connected ? data.altitude : 0;
    return new THREE.Vector3(
        (radius + altitude) * Math.cos(lat) * Math.cos(lon),
        (radius + altitude) * Math.cos(lat) * Math.sin(lon),
        (radius * (1 - 0.00669437999014) + altitude) * Math.sin(lat)
    ).applyMatrix4(tiles.group.matrixWorld);
}
function initializeTiles(data) {
    tiles = new TilesRenderer();
    tiles.registerPlugin(new GoogleCloudAuthPlugin({ apiToken: key, autoRefreshToken: true }));
    const draco = new DRACOLoader();
    draco.setDecoderPath("https://www.gstatic.com/draco/versioned/decoders/1.5.7/");
    tiles.registerPlugin(new GLTFExtensionsPlugin({ dracoLoader: draco, autoDispose: true }));
    reorientation = new ReorientationPlugin({ lat: radians(data.lat), lon: radians(data.lon) });
    tiles.registerPlugin(reorientation);
    terrain = new THREE.Group();
    terrain.rotation.y = Math.PI;
    terrain.add(tiles.group);
    scene.add(terrain);
    tiles.setCamera(camera);
    tiles.setResolutionFromRenderer(camera, renderer);
    tiles.lruCache.maxBytesSize = 256 * 1024 * 1024;
    tiles.addEventListener("load-root-tileset", () => {
        rootLoaded = true;
        terrain.updateMatrixWorld(true);
        applyPosition(lastPosition, true);
    });
    tiles.addEventListener("load-error", () => {
        if (!failedTiles) {
            failedTiles = true;
            error("Some Google 3D tiles failed to load. Check network access, billing and API restrictions.");
        }
    });
    tiles.addEventListener("load-model", () => {
        if (!failedTiles) notice.hidden = true;
    });
}
function applyPosition(data, reset = false) {
    if (!rootLoaded) return;
    terrain.updateMatrixWorld(true);
    targetPosition = earthPosition(data);
    targetEuler.set(radians(data.pitch), -radians(data.heading), -radians(data.roll), "YXZ");
    targetRotation = new THREE.Quaternion().setFromEuler(targetEuler);
    if (reset || !currentPosition) {
        currentPosition = targetPosition.clone();
        currentRotation = targetRotation.clone();
        groundHeight = undefined;
        lastGroundSample = 0;
        following = true;
        setRange(data.connected ? 60 : 600);
        currentRange = targetRange;
        controls.target.copy(currentPosition);
    }
}
async function initialize(apiKey) {
    key = apiKey;
    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
    renderer.setClearColor(0xb9d4e4);
    document.getElementById("map").appendChild(renderer.domElement);
    scene = new THREE.Scene();
    headingArrow = new THREE.ArrowHelper(new THREE.Vector3(0, 0, -1), new THREE.Vector3(), 3, 0xff5533);
    headingArrow.visible = false;
    scene.add(headingArrow);
    camera = new THREE.PerspectiveCamera(55, 1, 0.1, 100000);
    controls = new OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.12;
    controls.enableZoom = false;
    controls.minDistance = minimumRange;
    controls.maxDistance = maximumRange;
    controls.maxPolarAngle = Math.PI * 0.48;
    controls.addEventListener("start", () => { following = false; });
    scene.add(new THREE.HemisphereLight(0xffffff, 0x89939d, 2));
    const light = new THREE.DirectionalLight(0xffffff, 2.5);
    light.position.set(200, 400, 100);
    scene.add(light);
    try {
        const gltf = await new GLTFLoader().loadAsync("StarboundDrone.glb");
        drone = new THREE.Group();
        drone.add(gltf.scene);
        drone.visible = false;
        scene.add(drone);
        modelLoaded = true;
    } catch {
        error("Starbound drone model failed to load. Rebuild or reinstall the viewer assets.", "model-error");
    }
    renderer.domElement.addEventListener("wheel", event => {
        event.preventDefault();
        const delta = event.deltaY * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? renderer.domElement.clientHeight : 1);
        if (!following) {
            currentRange = camera.position.distanceTo(controls.target);
            targetRange = currentRange;
        }
        setRange(targetRange * Math.exp(THREE.MathUtils.clamp(delta * 0.0015, -1, 1)));
        following = true;
    }, { passive: false });
    new ResizeObserver(() => {
        const parent = document.getElementById("map");
        renderer.setSize(Math.max(1, parent.clientWidth), Math.max(1, parent.clientHeight));
        camera.aspect = Math.max(1, parent.clientWidth) / Math.max(1, parent.clientHeight);
        camera.updateProjectionMatrix();
    }).observe(document.getElementById("map"));
    renderer.setAnimationLoop(animate);
    report("tiles-ready");
}
function animate(now) {
    if (document.hidden) return;
    const dt = Math.min(0.1, Math.max(0.001, (now - (lastFrame || now - 16)) / 1000));
    lastFrame = now;
    const alpha = 1 - Math.exp(-dt * 8);
    if (targetPosition) {
        // Keep live vehicles above loaded terrain when reported altitude is below its surface.
        if (now - lastGroundSample > 1500 && tiles) {
            lastGroundSample = now;
            terrain.updateMatrixWorld(true);
            raycaster.set(new THREE.Vector3(targetPosition.x, 10000, targetPosition.z), down);
            const hits = raycaster.intersectObject(tiles.group, true);
            groundHeight = hits.length ? hits[0].point.y : undefined;
        }
        const adjusted = targetPosition.clone();
        if (Number.isFinite(groundHeight)) adjusted.y = Math.max(adjusted.y, groundHeight + (lastPosition.connected ? 1 : 0));
        currentPosition.lerp(adjusted, alpha);
        currentRotation.slerp(targetRotation, alpha);
        currentRange += (targetRange - currentRange) * alpha;
        const difference = ((targetOrbit - currentOrbit + 540) % 360) - 180;
        currentOrbit += difference * alpha;
        if (following) {
            controls.target.copy(currentPosition);
            const angle = radians(currentOrbit);
            const horizontal = currentRange * Math.cos(radians(30));
            camera.position.set(currentPosition.x + Math.sin(angle) * horizontal,
                currentPosition.y + currentRange * 0.5, currentPosition.z + Math.cos(angle) * horizontal);
            camera.lookAt(currentPosition);
        }
        if (drone) {
            drone.visible = lastPosition.connected;
            drone.position.copy(currentPosition);
            drone.quaternion.copy(currentRotation);
            const autoScale = camera.position.distanceTo(currentPosition) * 2 * Math.tan(radians(camera.fov / 2)) *
                80 / Math.max(1, renderer.domElement.clientHeight) / 0.221;
            drone.scale.setScalar(size.value === "auto" ? Math.max(1, autoScale) : Number(size.value));
        }
        headingArrow.visible = lastPosition.connected;
        if (headingArrow.visible) {
            const modelScale = drone ? drone.scale.x : 1;
            const direction = new THREE.Vector3(Math.sin(radians(lastPosition.heading)), 0, -Math.cos(radians(lastPosition.heading)));
            headingArrow.position.copy(currentPosition);
            headingArrow.position.y -= 0.04 * modelScale;
            headingArrow.setDirection(direction);
            const length = Math.max(3, 0.221 * modelScale * 1.7);
            headingArrow.setLength(length, length * 0.15, length * 0.08);
        }
    }
    controls.update();
    if (tiles) {
        camera.updateMatrixWorld();
        tiles.setResolutionFromRenderer(camera, renderer);
        tiles.update();
        document.getElementById("provider-credits").textContent = tiles.getAttributions()
            .filter(item => item.type === "string").map(item => item.value).filter(Boolean).join(" · ");
    }
    renderer.render(scene, camera);
    // Diagnostics contain counts only, never credentials or request URLs.
    document.getElementById("map").dataset.visibleTiles = tiles ? tiles.visibleTiles.size : 0;
    document.getElementById("map").dataset.modelVisible = Boolean(drone && drone.visible && modelLoaded);
    document.getElementById("map").dataset.range = currentRange.toFixed(2);
    document.getElementById("map").dataset.orbit = currentOrbit.toFixed(2);
    document.getElementById("map").dataset.headingVisible = headingArrow.visible;
    document.getElementById("map").dataset.headingBelowModel = Boolean(drone && headingArrow.position.y < drone.position.y);
}
orbit.addEventListener("input", () => {
    targetOrbit = Number(orbit.value);
    document.getElementById("orbit-value").textContent = orbit.value + "°";
    following = true;
});
document.getElementById("native").addEventListener("click", () => report("native"));
document.getElementById("reset").addEventListener("click", resetView);
zoom.addEventListener("input", () => {
    setRange(minimumRange * (maximumRange / minimumRange) ** (Number(zoom.value) / 100));
    following = true;
});
window.chrome.webview.addEventListener("message", async ({ data }) => {
    try {
        if (data.type === "initialize" && !initialized) {
            initialized = true;
            await initialize(data.key);
        } else if (data.type === "position" && renderer) {
            if (![data.lat, data.lon, data.altitude, data.heading, data.pitch, data.roll].every(Number.isFinite)) return;
            const reset = !lastPosition || lastPosition.connected !== data.connected;
            lastPosition = data;
            if (!tiles) initializeTiles(data);
            applyPosition(data, reset);
        }
    } catch {
        error("Google 3D viewer failed. Check network, API access and local viewer assets.");
    }
});
window.addEventListener("pagehide", () => {
    if (renderer) renderer.setAnimationLoop(null);
    if (tiles) tiles.dispose();
    if (controls) controls.dispose();
    if (renderer) renderer.dispose();
});
setRange(60);
report("ready");
