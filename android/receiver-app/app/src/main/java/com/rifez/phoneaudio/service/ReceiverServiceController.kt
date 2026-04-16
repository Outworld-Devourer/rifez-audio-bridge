package com.rifez.phoneaudio.service

import android.content.Context
import android.content.Intent
import androidx.core.content.ContextCompat

object ReceiverServiceController {

    fun start(context: Context) {
        val intent = Intent(context, ReceiverForegroundService::class.java).apply {
            action = ReceiverServiceActions.ACTION_START
        }
        ContextCompat.startForegroundService(context, intent)
    }

    fun stop(context: Context) {
        val intent = Intent(context, ReceiverForegroundService::class.java).apply {
            action = ReceiverServiceActions.ACTION_STOP
        }
        ContextCompat.startForegroundService(context, intent)
    }

    fun disconnectSession(context: Context) {
        val intent = Intent(context, ReceiverForegroundService::class.java).apply {
            action = ReceiverServiceActions.ACTION_DISCONNECT_SESSION
        }
        ContextCompat.startForegroundService(context, intent)
    }
}