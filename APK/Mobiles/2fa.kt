package com.jatin.runapp.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import com.jatin.runapp.data.MockServer
import com.jatin.runapp.data.TwoFactorMethod
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TwoFAScreen(navController: NavHostController) {
    val settings = remember { MockServer.getCurrentUserSettings() }
    
    var selectedMethod by remember { mutableStateOf<TwoFactorMethod?>(null) }
    var password by remember { mutableStateOf("") }
    var verificationCode by remember { mutableStateOf("") }
    
    var passwordError by remember { mutableStateOf("") }
    var codeError by remember { mutableStateOf("") }
    
    var showSetup by remember { mutableStateOf(false) }
    var showDisable by remember { mutableStateOf(false) }
    var isProcessing by remember { mutableStateOf(false) }
    var setupComplete by remember { mutableStateOf(false) }
    var showCodeInput by remember { mutableStateOf(false) }
    var countdown by remember { mutableIntStateOf(60) }
    val scope = rememberCoroutineScope()

    // Countdown for resend
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
                title = { Text("Two-Factor Authentication") },
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
            when {
                // 2FA is enabled - show status
                settings?.twoFactorEnabled == true && !showDisable && !showSetup -> {
                    TwoFAEnabledView(
                        method = settings.twoFactorMethod ?: TwoFactorMethod.SMS,
                        onDisableClick = { showDisable = true }
                    )
                }
                
                // Disable 2FA flow
                showDisable -> {
                    Disable2FAView(
                        password = password,
                        passwordError = passwordError,
                        isProcessing = isProcessing,
                        onPasswordChange = { password = it; passwordError = "" },
                        onDisable = {
                            if (password.isBlank()) {
                                passwordError = "Password is required"
                                return@Disable2FAView
                            }
                            isProcessing = true
                            scope.launch {
                                delay(1000)
                                val result = MockServer.disable2FA(password)
                                isProcessing = false
                                result.onSuccess {
                                    navController.popBackStack()
                                }.onFailure {
                                    passwordError = it.message ?: "Failed to disable 2FA"
                                }
                            }
                        },
                        onCancel = { showDisable = false }
                    )
                }
                
                // Setup complete
                setupComplete -> {
                    SetupCompleteView(
                        onDone = { navController.popBackStack() }
                    )
                }
                
                // Show verification code input
                showCodeInput -> {
                    CodeVerificationView(
                        method = selectedMethod!!,
                        code = verificationCode,
                        codeError = codeError,
                        countdown = countdown,
                        isProcessing = isProcessing,
                        onCodeChange = { verificationCode = it.filter { c -> c.isDigit() }.take(6); codeError = "" },
                        onVerify = {
                            if (verificationCode.length != 6) {
                                codeError = "Code must be 6 digits"
                                return@CodeVerificationView
                            }
                            isProcessing = true
                            scope.launch {
                                delay(1000)
                                val result = MockServer.verify2FASetup(verificationCode)
                                isProcessing = false
                                result.onSuccess {
                                    setupComplete = true
                                }.onFailure {
                                    codeError = it.message ?: "Invalid code"
                                }
                            }
                        },
                        onResend = {
                            if (countdown == 0) {
                                countdown = 60
                                // Resend code
                                scope.launch {
                                    MockServer.enable2FA(selectedMethod!!, password)
                                }
                            }
                        },
                        onBack = { showCodeInput = false; password = ""; verificationCode = "" }
                    )
                }
                
                // Show setup flow (password + QR for authenticator)
                showSetup -> {
                    when (selectedMethod) {
                        TwoFactorMethod.AUTHENTICATOR -> {
                            AuthenticatorSetupView(
                                password = password,
                                passwordError = passwordError,
                                isProcessing = isProcessing,
                                onPasswordChange = { password = it; passwordError = "" },
                                onContinue = {
                                    if (password.isBlank()) {
                                        passwordError = "Password is required"
                                        return@AuthenticatorSetupView
                                    }
                                    isProcessing = true
                                    scope.launch {
                                        delay(1000)
                                        val result = MockServer.enable2FA(TwoFactorMethod.AUTHENTICATOR, password)
                                        isProcessing = false
                                        result.onSuccess { secret ->
                                            showCodeInput = true
                                        }.onFailure {
                                            passwordError = it.message ?: "Failed to setup 2FA"
                                        }
                                    }
                                },
                                onBack = { showSetup = false; selectedMethod = null }
                            )
                        }
                        else -> {
                            SMS_Email_SetupView(
                                method = selectedMethod!!,
                                password = password,
                                passwordError = passwordError,
                                isProcessing = isProcessing,
                                onPasswordChange = { password = it; passwordError = "" },
                                onContinue = {
                                    if (password.isBlank()) {
                                        passwordError = "Password is required"
                                        return@SMS_Email_SetupView
                                    }
                                    isProcessing = true
                                    scope.launch {
                                        delay(1000)
                                        val result = MockServer.enable2FA(selectedMethod!!, password)
                                        isProcessing = false
                                        result.onSuccess {
                                            showCodeInput = true
                                        }.onFailure {
                                            passwordError = it.message ?: "Failed to send code"
                                        }
                                    }
                                },
                                onBack = { showSetup = false; selectedMethod = null }
                            )
                        }
                    }
                }
                
                // Choose method
                else -> {
                    MethodSelectionView(
                        selectedMethod = selectedMethod,
                        onMethodSelect = { selectedMethod = it },
                        onContinue = { showSetup = true }
                    )
                }
            }
        }
    }
}

// ========== SUB-COMPONENTS ==========

@Composable
fun TwoFAEnabledView(
    method: TwoFactorMethod,
    onDisableClick: () -> Unit
) {
    Column {
        Card(
            colors = CardDefaults.cardColors(
                containerColor = MaterialTheme.colorScheme.primaryContainer
            ),
            modifier = Modifier
                .fillMaxWidth()
                .padding(bottom = 24.dp)
        ) {
            Column(
                modifier = Modifier.padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(
                    text = "🔒",
                    style = MaterialTheme.typography.displayMedium
                )
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "2FA is Enabled",
                    style = MaterialTheme.typography.headlineSmall
                )
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = "Method: ${method.name}",
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onPrimaryContainer
                )
                Spacer(modifier = Modifier.height(4.dp))
                Text(
                    text = "Your account is protected with an extra layer of security.",
                    style = MaterialTheme.typography.bodyMedium,
                    textAlign = TextAlign.Center,
                    color = MaterialTheme.colorScheme.onPrimaryContainer.copy(alpha = 0.8f)
                )
            }
        }

        // Security tips
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(bottom = 24.dp)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    text = "Security Tips",
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.padding(bottom = 8.dp)
                )
                SecurityTip("Never share your 2FA codes with anyone")
                SecurityTip("Use a backup method in case you lose access")
                SecurityTip("Keep your recovery codes safe")
            }
        }

        Spacer(modifier = Modifier.weight(1f))

        Button(
            onClick = onDisableClick,
            colors = ButtonDefaults.buttonColors(
                containerColor = MaterialTheme.colorScheme.errorContainer,
                contentColor = MaterialTheme.colorScheme.onErrorContainer
            ),
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Disable 2FA")
        }
    }
}

@Composable
fun SecurityTip(text: String) {
    Row(
        modifier = Modifier.padding(vertical = 4.dp),
        verticalAlignment = Alignment.Top
    ) {
        Text(
            text = "•",
            style = MaterialTheme.typography.bodyLarge,
            modifier = Modifier.padding(end = 8.dp)
        )
        Text(
            text = text,
            style = MaterialTheme.typography.bodyMedium
        )
    }
}

@Composable
fun Disable2FAView(
    password: String,
    passwordError: String,
    isProcessing: Boolean,
    onPasswordChange: (String) -> Unit,
    onDisable: () -> Unit,
    onCancel: () -> Unit
) {
    Column {
        Card(
            colors = CardDefaults.cardColors(
                containerColor = MaterialTheme.colorScheme.errorContainer.copy(alpha = 0.3f)
            ),
            modifier = Modifier
                .fillMaxWidth()
                .padding(bottom = 24.dp)
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Text(
                    text = "⚠️ Warning",
                    style = MaterialTheme.typography.titleMedium,
                    color = MaterialTheme.colorScheme.error
                )
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "Disabling 2FA will make your account less secure. We recommend keeping it enabled.",
                    color = MaterialTheme.colorScheme.onErrorContainer
                )
            }
        }

        Text(
            text = "Enter your password to confirm",
            style = MaterialTheme.typography.bodyLarge,
            modifier = Modifier.padding(bottom = 16.dp)
        )

        OutlinedTextField(
            value = password,
            onValueChange = onPasswordChange,
            label = { Text("Current Password") },
            visualTransformation = PasswordVisualTransformation(),
            isError = passwordError.isNotEmpty(),
            supportingText = { 
                if (passwordError.isNotEmpty()) {
                    Text(passwordError, color = MaterialTheme.colorScheme.error)
                }
            },
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Password,
                imeAction = ImeAction.Done
            ),
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = onDisable,
            colors = ButtonDefaults.buttonColors(
                containerColor = MaterialTheme.colorScheme.error
            ),
            modifier = Modifier.fillMaxWidth(),
            enabled = !isProcessing
        ) {
            if (isProcessing) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = MaterialTheme.colorScheme.onError
                )
            } else {
                Text("Disable 2FA")
            }
        }

        Spacer(modifier = Modifier.height(8.dp))

        TextButton(
            onClick = onCancel,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Cancel")
        }
    }
}

@Composable
fun MethodSelectionView(
    selectedMethod: TwoFactorMethod?,
    onMethodSelect: (TwoFactorMethod) -> Unit,
    onContinue: () -> Unit
) {
    Column {
        Text(
            text = "Choose a 2FA Method",
            style = MaterialTheme.typography.headlineSmall,
            modifier = Modifier.padding(bottom = 8.dp)
        )
        Text(
            text = "Add an extra layer of security to your account",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(bottom = 24.dp)
        )

        MethodCard(
            icon = "📱",
            title = "SMS",
            description = "Receive a 6-digit code via text message",
            recommended = true,
            selected = selectedMethod == TwoFactorMethod.SMS,
            onClick = { onMethodSelect(TwoFactorMethod.SMS) }
        )

        Spacer(modifier = Modifier.height(12.dp))

        MethodCard(
            icon = "📧",
            title = "Email",
            description = "Receive a 6-digit code via email",
            recommended = false,
            selected = selectedMethod == TwoFactorMethod.EMAIL,
            onClick = { onMethodSelect(TwoFactorMethod.EMAIL) }
        )

        Spacer(modifier = Modifier.height(12.dp))

        MethodCard(
            icon = "🔐",
            title = "Authenticator App",
            description = "Use Google Authenticator, Authy, or similar",
            recommended = false,
            selected = selectedMethod == TwoFactorMethod.AUTHENTICATOR,
            onClick = { onMethodSelect(TwoFactorMethod.AUTHENTICATOR) }
        )

        Spacer(modifier = Modifier.weight(1f))

        Button(
            onClick = onContinue,
            modifier = Modifier.fillMaxWidth(),
            enabled = selectedMethod != null
        ) {
            Text("Continue")
        }
    }
}

@Composable
fun MethodCard(
    icon: String,
    title: String,
    description: String,
    recommended: Boolean,
    selected: Boolean,
    onClick: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick),
        colors = CardDefaults.cardColors(
            containerColor = if (selected) 
                MaterialTheme.colorScheme.primaryContainer 
            else 
                MaterialTheme.colorScheme.surface
        ),
        border = if (selected) {
            androidx.compose.foundation.BorderStroke(
                2.dp, 
                MaterialTheme.colorScheme.primary
            )
        } else null
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text(
                text = icon,
                style = MaterialTheme.typography.headlineMedium,
                modifier = Modifier.padding(end = 16.dp)
            )

            Column(modifier = Modifier.weight(1f)) {
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = title,
                        style = MaterialTheme.typography.titleMedium
                    )
                    if (recommended) {
                        Spacer(modifier = Modifier.width(8.dp))
                        Surface(
                            color = MaterialTheme.colorScheme.primary,
                            shape = MaterialTheme.shapes.small
                        ) {
                            Text(
                                text = "Recommended",
                                style = MaterialTheme.typography.labelSmall,
                                color = MaterialTheme.colorScheme.onPrimary,
                                modifier = Modifier.padding(horizontal = 8.dp, vertical = 2.dp)
                            )
                        }
                    }
                }
                Text(
                    text = description,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }

            RadioButton(
                selected = selected,
                onClick = onClick
            )
        }
    }
}

@Composable
fun SMS_Email_SetupView(
    method: TwoFactorMethod,
    password: String,
    passwordError: String,
    isProcessing: Boolean,
    onPasswordChange: (String) -> Unit,
    onContinue: () -> Unit,
    onBack: () -> Unit
) {
    Column {
        Text(
            text = "Setup ${method.name} 2FA",
            style = MaterialTheme.typography.headlineSmall,
            modifier = Modifier.padding(bottom = 8.dp)
        )
        Text(
            text = "Enter your password to continue",
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(bottom = 24.dp)
        )

        OutlinedTextField(
            value = password,
            onValueChange = onPasswordChange,
            label = { Text("Current Password") },
            visualTransformation = PasswordVisualTransformation(),
            isError = passwordError.isNotEmpty(),
            supportingText = { 
                if (passwordError.isNotEmpty()) {
                    Text(passwordError, color = MaterialTheme.colorScheme.error)
                }
            },
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Password,
                imeAction = ImeAction.Done
            ),
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = onContinue,
            modifier = Modifier.fillMaxWidth(),
            enabled = !isProcessing
        ) {
            if (isProcessing) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = MaterialTheme.colorScheme.onPrimary
                )
            } else {
                Text("Send Verification Code")
            }
        }

        Spacer(modifier = Modifier.height(8.dp))

        TextButton(
            onClick = onBack,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Back")
        }
    }
}

@Composable
fun AuthenticatorSetupView(
    password: String,
    passwordError: String,
    isProcessing: Boolean,
    onPasswordChange: (String) -> Unit,
    onContinue: () -> Unit,
    onBack: () -> Unit
) {
    Column {
        Text(
            text = "Setup Authenticator App",
            style = MaterialTheme.typography.headlineSmall,
            modifier = Modifier.padding(bottom = 8.dp)
        )

        // QR Code Card
        Card(
            modifier = Modifier
                .fillMaxWidth()
                .padding(vertical = 16.dp)
        ) {
            Column(
                modifier = Modifier.padding(24.dp),
                horizontalAlignment = Alignment.CenterHorizontally
            ) {
                Text(
                    text = "Step 1: Enter Password",
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.padding(bottom = 16.dp)
                )

                OutlinedTextField(
                    value = password,
                    onValueChange = onPasswordChange,
                    label = { Text("Current Password") },
                    visualTransformation = PasswordVisualTransformation(),
                    isError = passwordError.isNotEmpty(),
                    supportingText = { 
                        if (passwordError.isNotEmpty()) {
                            Text(passwordError, color = MaterialTheme.colorScheme.error)
                        }
                    },
                    keyboardOptions = KeyboardOptions(
                        keyboardType = KeyboardType.Password,
                        imeAction = ImeAction.Done
                    ),
                    modifier = Modifier.fillMaxWidth()
                )

                Spacer(modifier = Modifier.height(16.dp))

                Text(
                    text = "Step 2: Scan QR Code",
                    style = MaterialTheme.typography.titleMedium
                )
                Spacer(modifier = Modifier.height(8.dp))
                Text(
                    text = "Open your authenticator app and scan this code",
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    textAlign = TextAlign.Center
                )

                Spacer(modifier = Modifier.height(16.dp))

                // QR Code Placeholder
                Surface(
                    color = MaterialTheme.colorScheme.surfaceVariant,
                    modifier = Modifier.size(200.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Column(horizontalAlignment = Alignment.CenterHorizontally) {
                            Text(
                                text = "🔲",
                                style = MaterialTheme.typography.displayLarge
                            )
                            Spacer(modifier = Modifier.height(8.dp))
                            Text(
                                text = "QR Code",
                                style = MaterialTheme.typography.bodySmall
                            )
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                Text(
                    text = "Can't scan? Enter this key manually:",
                    style = MaterialTheme.typography.bodyMedium
                )
                Spacer(modifier = Modifier.height(4.dp))
                Surface(
                    color = MaterialTheme.colorScheme.surfaceVariant,
                    shape = MaterialTheme.shapes.small
                ) {
                    Text(
                        text = "JBSW Y3DP EHPK 3PXP",
                        style = MaterialTheme.typography.bodyLarge,
                        modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
                    )
                }
            }
        }

        Spacer(modifier = Modifier.weight(1f))

        Button(
            onClick = onContinue,
            modifier = Modifier.fillMaxWidth(),
            enabled = !isProcessing && password.isNotBlank()
        ) {
            if (isProcessing) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = MaterialTheme.colorScheme.onPrimary
                )
            } else {
                Text("I've Scanned the Code")
            }
        }

        Spacer(modifier = Modifier.height(8.dp))

        TextButton(
            onClick = onBack,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Back")
        }
    }
}

@Composable
fun CodeVerificationView(
    method: TwoFactorMethod,
    code: String,
    codeError: String,
    countdown: Int,
    isProcessing: Boolean,
    onCodeChange: (String) -> Unit,
    onVerify: () -> Unit,
    onResend: () -> Unit,
    onBack: () -> Unit
) {
    Column(
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = "Enter Verification Code",
            style = MaterialTheme.typography.headlineSmall,
            modifier = Modifier.padding(bottom = 8.dp)
        )
        Text(
            text = when (method) {
                TwoFactorMethod.SMS -> "We sent a 6-digit code to your phone"
                TwoFactorMethod.EMAIL -> "We sent a 6-digit code to your email"
                TwoFactorMethod.AUTHENTICATOR -> "Enter the 6-digit code from your authenticator app"
            },
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center,
            modifier = Modifier.padding(bottom = 32.dp)
        )

        // Code input boxes
        Row(
            modifier = Modifier.padding(bottom = 16.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp)
        ) {
            repeat(6) { index ->
                val digit = code.getOrNull(index)?.toString() ?: ""
                Surface(
                    shape = MaterialTheme.shapes.small,
                    color = if (digit.isNotEmpty()) 
                        MaterialTheme.colorScheme.primaryContainer 
                    else 
                        MaterialTheme.colorScheme.surfaceVariant,
                    modifier = Modifier.size(48.dp),
                    border = androidx.compose.foundation.BorderStroke(
                        1.dp,
                        if (codeError.isNotEmpty()) 
                            MaterialTheme.colorScheme.error 
                        else 
                            MaterialTheme.colorScheme.outline
                    )
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Text(
                            text = digit,
                            style = MaterialTheme.typography.headlineMedium
                        )
                    }
                }
            }
        }

        // Hidden actual text field for input
        OutlinedTextField(
            value = code,
            onValueChange = onCodeChange,
            label = { Text("Code") },
            isError = codeError.isNotEmpty(),
            supportingText = { 
                if (codeError.isNotEmpty()) {
                    Text(codeError, color = MaterialTheme.colorScheme.error)
                }
            },
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Number,
                imeAction = ImeAction.Done
            ),
            modifier = Modifier.fillMaxWidth()
        )

        Spacer(modifier = Modifier.height(8.dp))

        // Resend option
        TextButton(
            onClick = onResend,
            enabled = countdown == 0,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text(
                if (countdown > 0) "Resend code in ${countdown}s" else "Resend code"
            )
        }

        Spacer(modifier = Modifier.weight(1f))

        Button(
            onClick = onVerify,
            modifier = Modifier.fillMaxWidth(),
            enabled = code.length == 6 && !isProcessing
        ) {
            if (isProcessing) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = MaterialTheme.colorScheme.onPrimary
                )
            } else {
                Text("Verify & Enable")
            }
        }

        Spacer(modifier = Modifier.height(8.dp))

        TextButton(
            onClick = onBack,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Back")
        }
    }
}

@Composable
fun SetupCompleteView(onDone: () -> Unit) {
    Column(
        modifier = Modifier.fillMaxSize(),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center
    ) {
        Text(
            text = "🎉",
            style = MaterialTheme.typography.displayLarge
        )
        Spacer(modifier = Modifier.height(16.dp))
        Text(
            text = "2FA Enabled!",
            style = MaterialTheme.typography.headlineMedium
        )
        Spacer(modifier = Modifier.height(8.dp))
        Text(
            text = "Your account is now more secure.",
            style = MaterialTheme.typography.bodyLarge,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            textAlign = TextAlign.Center
        )
        Spacer(modifier = Modifier.height(32.dp))
        Button(
            onClick = onDone,
            modifier = Modifier.fillMaxWidth()
        ) {
            Text("Done")
        }
    }
}
