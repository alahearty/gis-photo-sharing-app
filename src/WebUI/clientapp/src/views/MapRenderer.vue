<template>
<div class="row">
    <div v-if="loading" class="text-center py-4">Loading map...</div>
    <div v-if="error" class="alert alert-warning mx-3">{{ error }}</div>
    <div id="map" style="width:100%; height:800px; margin-top:10px; min-height:400px;"></div>
</div>
</template>

<script scoped>
import mapboxgl from "mapbox-gl";
export default {
    name: "baseMap",
    data() {
        return {
            map: null,
            markers: [],
            loading: true,
            error: null,
        };
    },
    mounted() {
        // Use env variable - set VUE_APP_MAPBOX_TOKEN in .env.local (never commit secrets)
        mapboxgl.accessToken = process.env.VUE_APP_MAPBOX_TOKEN || "";
        this.map = new mapboxgl.Map({
            container: "map",
            style: "mapbox://styles/mapbox/streets-v12",
            center: [-74.5, 40],
            zoom: 9,
        });
        this.map.addControl(new mapboxgl.NavigationControl(), "top-right");
        this.loadPhotos();
    },
    methods: {
        async loadPhotos() {
            try {
                this.loading = true;
                this.error = null;
                const baseUrl = process.env.VUE_APP_API_URL || "";
                const res = await fetch(`${baseUrl}/api/photos`);
                if (res.ok) {
                    const photos = await res.json();
                    this.addPhotoMarkers(photos);
                } else {
                    this.error = "Could not load photos.";
                }
            } catch (e) {
                this.error = "Could not connect to the API. Is it running?";
                console.warn("Could not load photos for map:", e);
            } finally {
                this.loading = false;
            }
        },
        addPhotoMarkers(photos) {
            this.markers.forEach((m) => m.remove());
            this.markers = [];
            const baseUrl = process.env.VUE_APP_API_URL || "";
            (photos || []).forEach((p) => {
                if (p.latitude != null && p.longitude != null) {
                    const el = document.createElement("div");
                    el.className = "photo-marker";
                    el.style.width = "24px";
                    el.style.height = "24px";
                    el.style.borderRadius = "50%";
                    el.style.background = "#3388ff";
                    el.style.border = "2px solid white";
                    el.style.cursor = "pointer";
                    const imgHtml = p.filePath
                        ? `<img src="${baseUrl}${p.filePath}" alt="${(p.title || "").replace(/"/g, "&quot;")}" style="max-width:200px;max-height:150px;border-radius:4px;margin:4px 0;" onerror="this.style.display='none'"/>`
                        : "";
                    const popup = new mapboxgl.Popup({ offset: 25, maxWidth: "280px" }).setHTML(
                        `${imgHtml}<strong>${(p.title || "Photo").replace(/</g, "&lt;")}</strong><br/><span style="font-size:12px;color:#666">${(p.description || "").replace(/</g, "&lt;")}</span>`
                    );
                    const marker = new mapboxgl.Marker(el)
                        .setLngLat([p.longitude, p.latitude])
                        .setPopup(popup)
                        .addTo(this.map);
                    this.markers.push(marker);
                }
            });
        },
    },
};
</script>

<style scoped>
.basemapd {
    width: 100%;
    height: 100%;
}
</style>
