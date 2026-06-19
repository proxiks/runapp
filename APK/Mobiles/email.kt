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
fun EmailSettingsScreen(navController: NavHostController) {
    val currentSettings = remember { MockServer.getCurrentUserSettings() }
    
    var newEmail by remember { mutableStateOf("") }
    var currentPassword by remember { mutableStateOf("") }
    var verificationCode by remember { mutableStateOf("") }
    
    var emailError by remember { mutableStateOf("") }
    var passwordError by remember { mutableStateOf("") }
    var codeError by remember { mutableStateOf("") }
    
    var isVerifying by remember { mutableStateOf(false) }
    var showCodeInput by remember { mutableStateOf(false) }
    var isSuccess by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Email Settings") },
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
            // Current email display
            Card(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(bottom = 16.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    Text(
                        text = "Current Email",
                        style = MaterialTheme.typography.labelLarge
                    )
                    Text(
                        text = currentSettings?.email ?: "",
                        style = MaterialTheme.typography.bodyLarge
                    )
                    if (currentSettings?.isEmailVerified == true) {
                        Text(
                            text = "✓ Verified",
                            color = MaterialTheme.colorScheme.primary,
                            style = MaterialTheme.typography.bodyMedium
                        )
                    } else {
                        Text(
                            text = "⚠ Not verified",
                            color = MaterialTheme.colorScheme.error,
                            style = MaterialTheme.typography.bodyMedium
                        )
                    }
                }
            }

            if (isSuccess) {
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.primaryContainer
                    ),
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(bottom = 16.dp)
                ) {
                    Text(
                        text = "✅ Email updated! Please check your inbox to verify.",
                        modifier = Modifier.padding(16.dp)
                    )
                }
            }

            if (!showCodeInput) {
                // New email field
                OutlinedTextField(
                    value = newEmail,
                    onValueChange = { 
                        newEmail = it
                        emailError = ""
                    },
                    label = { Text("New Email Address") },
                    isError = emailError.isNotEmpty(),
                    supportingText = { 
                        if (emailError.isNotEmpty()) {
                            Text(emailError, color = MaterialTheme.colorScheme.error)
                        } else {
                            Text("We'll send a verification code to this email")
                        }
                    },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Email,
                        imeAction = ImeAction.Next
                    ),
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Current password
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
                        // Validate email
                        val emailVal = ValidationUtils.validateEmail(newEmail)
                        if (!emailVal.isValid) {
                            emailError = emailVal.errorMessage ?: "Invalid email"
                            return@Button
                        }
                        
                        if (currentPassword.isBlank()) {
                            passwordError = "Password is required"
                            return@Button
                        }

                        isVerifying = true
                        scope.launch {
                            delay(1000)
                            val result = MockServer.updateEmail(newEmail, currentPassword)
                            isVerifying = false
                            result.onSuccess {
                                showCodeInput = true
                            }.onFailure {
                                emailError = it.message ?: "Failed to update email"
                            }
                        }
                    },
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(48.dp),
                    enabled = !isVerifying && newEmail.isNotBlank()
                ) {
                    if (isVerifying) {
                        CircularProgressIndicator(
                            modifier = Modifier.size(24.dp),
                            color = MaterialTheme.colorScheme.onPrimary
                        )
                    } else {
                        Text("Send Verification Code")
                    }
                }
            } else {
                // Verification code input
                Text(
                    text = "We sent a code to $newEmail",
                    style = MaterialTheme.typography.bodyLarge,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                OutlinedTextField(
                    value = verificationCode,
                    onValueChange = { 
                        verificationCode = it.filter { c -> c.isDigit() }.take(6)
                        codeError = ""
                    },
                    label = { Text("Verification Code") },
                    isError = codeError.isNotEmpty(),
                    supportingText = { 
                        if (codeError.isNotEmpty()) Text(codeError, color = MaterialTheme.colorScheme.error)
                        else Text("Enter the 6-digit code")
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
                        if (verificationCode.length != 6) {
                            codeError = "Code must be 6 digits"
                            return@Button
                        }

                        isVerifying = true
                        scope.launch {
                            delay(1000)
                            val result = MockServer.verifyEmail(verificationCode)
                            isVerifying = false
                            result.onSuccess {
                                isSuccess = true
                                showCodeInput = false
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
                        Text("Verify Email")
                    }
                }

                Spacer(modifier = Modifier.height(8.dp))

                TextButton(
                    onClick = { showCodeInput = false },
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Text("Change Email Address")
                }
            }
        }
    }
}