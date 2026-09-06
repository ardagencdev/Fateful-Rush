package com.youngdevstudios.fatefulrush.compat;

import android.app.Activity;
import android.view.Window;

import androidx.core.view.WindowCompat;

public final class FatefulRushEdgeToEdge
{
    private FatefulRushEdgeToEdge() {}

    public static void apply(Activity activity)
    {
        if (activity == null)
            return;

        activity.runOnUiThread(() ->
        {
            Window window = activity.getWindow();

            if (window == null)
                return;

            WindowCompat.setDecorFitsSystemWindows(window, false);
        });
    }
}
