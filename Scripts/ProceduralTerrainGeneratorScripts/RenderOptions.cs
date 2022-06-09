using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RenderOptions {
    public static void UpdateTenderOptions(bool fog, int clippingPlanes) {
        RenderSettings.fog = fog;
        Camera.main.farClipPlane = clippingPlanes;
    }
}
