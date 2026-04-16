package com.rifez.phoneaudio.protocol

sealed class ReceiverCommand {
    data class Hello(val clientName: String?) : ReceiverCommand()
    data object Ping : ReceiverCommand()
    data object GetStatus : ReceiverCommand()
    data object StreamStart : ReceiverCommand()
    data object Disconnect : ReceiverCommand()
    data class Unknown(val raw: String) : ReceiverCommand()
}