package com.youngdevstudios.fatefulrush.compat;

import android.app.Activity;
import android.os.Build;
import android.view.Window;
import android.view.WindowManager;

import androidx.core.view.WindowCompat;

/**
 * Small native window helper for Fateful Rush.
 *
 * It deliberately does NOT use
 * LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES.
 * On API 30+ it uses ALWAYS, and on older Android versions the normal
 * edge-to-edge decor policy is used without forcing a deprecated cutout mode.
 */
public final class FatefulRushWindowCompat
{
    private FatefulRushWindowCompat() {}

    public static void apply(Activity activity)
    {
        if (activity == null)
            return;

        Window window = activity.getWindow();

        // Modern edge-to-edge mechanism. Works on old Android through AndroidX
        // without forcing the deprecated SHORT_EDGES cutout parameter here.
        WindowCompat.setDecorFitsSystemWindows(window, false);

        if (Build.VERSION.SDK_INT >= 30)
        {
            WindowManager.LayoutParams attributes =
                window.getAttributes();

            attributes.layoutInDisplayCutoutMode =
                WindowManager.LayoutParams
                    .LAYOUT_IN_DISPLAY_CUTOUT_MODE_ALWAYS;

            window.setAttributes(attributes);
        }
    }
}
