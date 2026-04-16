package com.rifez.phoneaudio.discovery

import android.content.Context
import android.net.nsd.NsdManager
import android.net.nsd.NsdServiceInfo
import android.util.Log

class NsdRegistrar(
    context: Context
) {
    private val nsdManager =
        context.getSystemService(Context.NSD_SERVICE) as NsdManager

    private var registrationListener: NsdManager.RegistrationListener? = null
    private var currentServiceInfo: NsdServiceInfo? = null

    var state: NsdRegistrationState = NsdRegistrationState.Idle
        private set

    fun register(
        serviceName: String,
        serviceType: String = SERVICE_TYPE,
        port: Int = DEFAULT_PORT,
        onStateChanged: (NsdRegistrationState) -> Unit
    ) {
        unregister()

        val serviceInfo = NsdServiceInfo().apply {
            this.serviceName = serviceName
            this.serviceType = serviceType
            this.port = port
        }

        currentServiceInfo = serviceInfo
        state = NsdRegistrationState.Registering(serviceName, port)
        onStateChanged(state)

        val listener = object : NsdManager.RegistrationListener {
            override fun onServiceRegistered(nsdServiceInfo: NsdServiceInfo) {
                currentServiceInfo = nsdServiceInfo
                state = NsdRegistrationState.Registered(
                    serviceName = nsdServiceInfo.serviceName,
                    port = nsdServiceInfo.port
                )
                Log.d(TAG, "NSD registered: ${nsdServiceInfo.serviceName} (callback port=${nsdServiceInfo.port})")
                onStateChanged(state)
            }

            override fun onRegistrationFailed(serviceInfo: NsdServiceInfo, errorCode: Int) {
                state = NsdRegistrationState.Failed(
                    serviceName = serviceInfo.serviceName,
                    port = serviceInfo.port,
                    errorCode = errorCode,
                    message = "NSD registration failed"
                )
                Log.e(TAG, "NSD registration failed for ${serviceInfo.serviceName}, error=$errorCode")
                onStateChanged(state)
            }

            override fun onServiceUnregistered(serviceInfo: NsdServiceInfo) {
                state = NsdRegistrationState.Idle
                Log.d(TAG, "NSD unregistered: ${serviceInfo.serviceName}")
                onStateChanged(state)
            }

            override fun onUnregistrationFailed(serviceInfo: NsdServiceInfo, errorCode: Int) {
                state = NsdRegistrationState.Failed(
                    serviceName = serviceInfo.serviceName,
                    port = serviceInfo.port,
                    errorCode = errorCode,
                    message = "NSD unregistration failed"
                )
                Log.e(TAG, "NSD unregistration failed for ${serviceInfo.serviceName}, error=$errorCode")
                onStateChanged(state)
            }
        }

        registrationListener = listener

        nsdManager.registerService(
            serviceInfo,
            NsdManager.PROTOCOL_DNS_SD,
            listener
        )
    }

    fun unregister() {
        val listener = registrationListener ?: return
        try {
            nsdManager.unregisterService(listener)
        } catch (e: IllegalArgumentException) {
            Log.w(TAG, "NSD unregister ignored: ${e.message}")
        } finally {
            registrationListener = null
            currentServiceInfo = null
            state = NsdRegistrationState.Idle
        }
    }

    companion object {
        private const val TAG = "NsdRegistrar"
        const val SERVICE_TYPE = "_rifezaudio._tcp"
        const val DEFAULT_PORT = 49521
    }
}