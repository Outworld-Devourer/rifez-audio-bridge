package com.rifez.phoneaudio.service

import android.content.Context
import androidx.core.content.edit

class ReceiverPreferences(context: Context) {
    private val prefs = context.getSharedPreferences("receiver_prefs", Context.MODE_PRIVATE)

    fun getDeviceName(defaultValue: String = "Kiril Phone"): String {
        return prefs.getString(KEY_DEVICE_NAME, defaultValue) ?: defaultValue
    }

    fun setDeviceName(value: String) {
        prefs.edit { putString(KEY_DEVICE_NAME, value) }
    }

    companion object {
        private const val KEY_DEVICE_NAME = "device_name"
    }
}