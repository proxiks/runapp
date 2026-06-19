package com.jatin.runapp.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import com.jatin.runapp.data.MockServer
import com.jatin.runapp.utils.ValidationUtils
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PhoneSettingsScreen(navController: NavHostController) {
    val currentSettings = remember { MockServer.getCurrentUserSettings() }
    
    var phoneNumber by remember { mutableStateOf(currentSettings?.phoneNumber ?: "") }
    var currentPassword by remember { mutableStateOf("") }
    var verificationCode by remember { mutableStateOf("") }
    
    var phoneError by remember { mutableStateOf("") }
    var passwordError by remember { mutableStateOf("") }
    var codeError by remember { mutableStateOf("") }
    
    var isVerifying by remember { mutableStateOf(false) }
    var showCodeInput by remember { mutableStateOf(false) }
    var countdown by remember { mutableIntStateOf(60) }
    val scope = rememberCoroutineScope()

    // Countdown timer
    LaunchedEffect(showCodeInput) {
        if (showCodeInput) {
            countdown = 60
            while (countdown > 0) {
                delay(1000)
                countdown--
            }
        }
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Phone Number") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(16.dp)
        ) {
            if (!showCodeInput) {
                OutlinedTextField(
                    value = phoneNumber,
                    onValueChange = { 
                        phoneNumber = it
                        phoneError = ""
                    },
                    label = { Text("Phone Number") },
                    placeholder = { Text("+1 234 567 8900") },
                    isError = phoneError.isNotEmpty(),
                    supportingText = { 
                        if (phoneError.isNotEmpty()) Text(phoneError, color = MaterialTheme.colorScheme.error)
                        else Text("Include country code (e.g., +91 for India)")
                    },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Phone,
                        imeAction = ImeAction.Next
                    ),
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                OutlinedTextField(
                    value = currentPassword,
                    onValueChange = { 
                        currentPassword = it
                        passwordError = ""
                    },
                    label = { Text("Current Password") },
                    visualTransformation = PasswordVisualTransformation(),
                    isError = passwordError.isNotEmpty(),
                    supportingText = { if (passwordError.isNotEmpty()) Text(passwordError, color = MaterialTheme.colorScheme.error) },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Password,
                        imeAction = ImeAction.Done
                    ),
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(24.dp))

                Button(
                    onClick = {
                        val phoneVal = ValidationUtils.validatePhoneNumber(phoneNumber)
                        if (!phoneVal.isValid) {
                            phoneError = phoneVal.errorMessage ?: "Invalid phone"
                            return@Button
                        }
                        
                        if (currentPassword.isBlank()) {
                            passwordError = "Password is required"
                            return@Button
                        }

                        isVerifying = true
                        scope.launch {
                            delay(1000)
                            val result = MockServer.updatePhoneNumber(phoneNumber, currentPassword)
                            isVerifying = false
                            result.onSuccess {
                                showCodeInput = true
                            }.onFailure {
                                phoneError = it.message ?: "Failed to update phone"
                            }
                        }
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(48.dp),
                    enabled = !isVerifying
                ) {
                    if (isVerifying) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(24.dp),
                            color = MaterialTheme.colorScheme.onPrimary
                        )
                    } else {
                        Text("Send OTP")
                    }
                }
            } else {
                Text(
                    text = "Enter the 6-digit code sent to $phoneNumber",
                    style = MaterialTheme.typography.bodyLarge,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                OutlinedTextField(
                    value = verificationCode,
                    onValueChange = { 
                        verificationCode = it.filter { c -> c.isDigit() }.take(6)
                        codeError = ""
                    },
                    label = { Text("OTP Code") },
                    isError = codeError.isNotEmpty(),
                    supportingText = { 
                        if (codeError.isNotEmpty()) Text(codeError, color = MaterialTheme.colorScheme.error)
                    },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Number,
                        imeAction = ImeAction.Done
                    ),
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                Button(
                    onClick = {
                        isVerifying = true
                        scope.launch {
                            delay(1000)
                            val result = MockServer.verifyPhone(verificationCode)
                            isVerifying = false
                            result.onSuccess {
                                navController.popBackStack()
                            }.onFailure {
                                codeError = it.message ?: "Invalid code"
                            }
                        }
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(48.dp),
                    enabled = !isVerifying && verificationCode.length == 6
                ) {
                    if (isVerifying) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(24.dp),
                            color = MaterialTheme.colorScheme.onPrimary
                        )
                    } else {
                        Text("Verify")
                    }
                }

                Spacer(modifier = Modifier.height(8.dp))

                TextButton(
                    onClick = { 
                        if (countdown == 0) {
                            // Resend
                            countdown = 60
                        }
                    },
                    enabled = countdown == 0,
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text(if (countdown > 0) "Resend in ${countdown}s" else "Resend OTP")
                }

                TextButton(
                    onClick = { showCodeInput = false },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Change Phone Number")
                }
            }
        }
    }
}