package com.rifez.phoneaudio.stream

sealed class ReceiverSocketState {
    data object Idle : ReceiverSocketState()
    data class Listening(val port: Int) : ReceiverSocketState()
    data class ClientConnected(
        val port: Int,
        val remoteHost: String,
        val remotePort: Int
    ) : ReceiverSocketState()
    data class Failed(val message: String) : ReceiverSocketState()
}