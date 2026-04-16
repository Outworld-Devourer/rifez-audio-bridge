package com.rifez.phoneaudio.discovery

sealed class NsdRegistrationState {
    data object Idle : NsdRegistrationState()
    data class Registering(val serviceName: String, val port: Int) : NsdRegistrationState()
    data class Registered(val serviceName: String, val port: Int) : NsdRegistrationState()
    data class Failed(val serviceName: String?, val port: Int?, val errorCode: Int?, val message: String) : NsdRegistrationState()
}