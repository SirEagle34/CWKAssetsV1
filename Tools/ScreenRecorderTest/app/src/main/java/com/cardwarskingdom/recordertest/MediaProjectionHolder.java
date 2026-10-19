package com.cardwarskingdom.recordertest;

import android.media.projection.MediaProjection;

/**
 * Holder estático que permite pasar un MediaProjection de la Activity al Service.
 *
 * En Android 14+/15, getMediaProjection() DEBE llamarse desde el mismo contexto
 * donde el usuario otorgó el permiso (onActivityResult de la Activity), no desde
 * un servicio. Este holder puente el objeto ya construido al servicio.
 */
public class MediaProjectionHolder {
    private static volatile MediaProjection projection;

    /** Llamar desde MainActivity.onActivityResult ANTES de iniciar el servicio. */
    public static void set(MediaProjection mp) {
        projection = mp;
    }

    /** Llamar desde RecordingService.onStartCommand — consume y limpia la referencia. */
    public static MediaProjection consume() {
        MediaProjection mp = projection;
        projection = null;
        return mp;
    }

    public static boolean hasProjection() {
        return projection != null;
    }
}
