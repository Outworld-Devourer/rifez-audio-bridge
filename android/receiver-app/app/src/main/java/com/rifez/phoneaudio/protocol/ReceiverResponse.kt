package com.rifez.phoneaudio.protocol

sealed class ReceiverResponse {
    data class Ok(val action: String, val payload: String? = null) : ReceiverResponse()
    data class Error(val reason: String) : ReceiverResponse()

    fun asWireLine(): String {
        return when (this) {
            is Ok -> {
                if (payload.isNullOrBlank()) {
                    "OK $action"
                } else {
                    "OK $action $payload"
                }
            }

            is Error -> "ERR $reason"
        }
    }
}