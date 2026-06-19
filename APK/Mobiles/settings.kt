package com.jatin.jatinbook.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.ExitToApp
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import com.jatin.jatinbook.data.*
import com.jatin.jatinbook.utils.ValidationUtils
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreen(navController: NavHostController) {
    val settings = remember { MockServer.getCurrentUserSettings() }
    var showLogoutDialog by remember { mutableStateOf(false) }
    var showDeleteDialog by remember { mutableStateOf(false) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Settings") },
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
                .verticalScroll(rememberScrollState())
        ) {
            // Profile Section
            ProfileSection(navController)

            Divider(modifier = Modifier.padding(vertical = 8.dp))

            // Account Settings
            SettingsCategory("Account") {
                SettingsItem(
                    icon = Icons.Default.Person,
                    title = "Edit Profile",
                    subtitle = "Name, username, bio",
                    onClick = { navController.navigate("settings_edit_profile") }
                )
                SettingsItem(
                    icon = Icons.Default.Email,
                    title = "Email",
                    subtitle = settings?.email ?: "",
                    trailing = {
                        if (settings?.isEmailVerified == true) {
                            Badge(containerColor = MaterialTheme.colorScheme.primary) {
                                Text("✓", color = Color.White)
                            }
                        } else {
                            Text("Verify", color = MaterialTheme.colorScheme.error)
                        }
                    },
                    onClick = { navController.navigate("settings_email") }
                )
                SettingsItem(
                    icon = Icons.Default.Phone,
                    title = "Phone Number",
                    subtitle = settings?.phoneNumber ?: "Not added",
                    trailing = {
                        if (settings?.isPhoneVerified == true) {
                            Badge(containerColor = MaterialTheme.colorScheme.primary) {
                                Text("✓", color = Color.White)
                            }
                        }
                    },
                    onClick = { navController.navigate("settings_phone") }
                )
                SettingsItem(
                    icon = Icons.Default.Lock,
                    title = "Change Password",
                    subtitle = "Update your password",
                    onClick = { navController.navigate("settings_password") }
                )
            }

            Divider(modifier = Modifier.padding(vertical = 8.dp))

            // Security
            SettingsCategory("Security") {
                SettingsItem(
                    icon = Icons.Default.Security,
                    title = "Two-Factor Authentication",
                    subtitle = if (settings?.twoFactorEnabled == true) "Enabled" else "Not enabled",
                    trailing = {
                        Switch(
                            checked = settings?.twoFactorEnabled ?: false,
                            onCheckedChange = { navController.navigate("settings_2fa") }
                        )
                    },
                    onClick = { navController.navigate("settings_2fa") }
                )
                SettingsItem(
                    icon = Icons.Default.Visibility,
                    title = "Privacy",
                    subtitle = settings?.privacyLevel?.name ?: "Public",
                    onClick = { navController.navigate("settings_privacy") }
                )
            }

            Divider(modifier = Modifier.padding(vertical = 8.dp))

            // Preferences
            SettingsCategory("Preferences") {
                SettingsItem(
                    icon = Icons.Default.Notifications,
                    title = "Notifications",
                    subtitle = if (settings?.notificationsEnabled != false) "On" else "Off",
                    trailing = {
                        Switch(
                            checked = settings?.notificationsEnabled ?: true,
                            onCheckedChange = { enabled ->
                                MockServer.updateNotifications(enabled)
                            }
                        )
                    },
                    onClick = {}
                )
                SettingsItem(
                    icon = Icons.Default.DarkMode,
                    title = "Dark Mode",
                    subtitle = "App theme",
                    trailing = {
                        Switch(
                            checked = settings?.darkMode ?: false,
                            onCheckedChange = { enabled ->
                                MockServer.updateDarkMode(enabled)
                            }
                        )
                    },
                    onClick = {}
                )
            }

            Divider(modifier = Modifier.padding(vertical = 8.dp))

            // Danger Zone
            SettingsCategory("Danger Zone", color = MaterialTheme.colorScheme.error) {
                SettingsItem(
                    icon = Icons.AutoMirrored.Filled.ExitToApp,
                    title = "Log Out",
                    titleColor = MaterialTheme.colorScheme.error,
                    onClick = { showLogoutDialog = true }
                )
                SettingsItem(
                    icon = Icons.Default.DeleteForever,
                    title = "Delete Account",
                    titleColor = MaterialTheme.colorScheme.error,
                    onClick = { showDeleteDialog = true }
                )
            }

            Spacer(modifier = Modifier.height(32.dp))
        }
    }

    // Logout Dialog
    if (showLogoutDialog) {
        AlertDialog(
            onDismissRequest = { showLogoutDialog = false },
            title = { Text("Log Out") },
            text = { Text("Are you sure you want to log out?") },
            confirmButton = {
                TextButton(
                    onClick = {
                        MockServer.logout()
                        navController.navigate("login") {
                            popUpTo(0) { inclusive = true }
                        }
                    },
                    colors = ButtonDefaults.textButtonColors(contentColor = MaterialTheme.colorScheme.error)
                ) {
                    Text("Log Out")
                }
            },
            dismissButton = {
                TextButton(onClick = { showLogoutDialog = false }) {
                    Text("Cancel")
                }
            }
        )
    }

    // Delete Account Dialog
    if (showDeleteDialog) {
        DeleteAccountDialog(
            onDismiss = { showDeleteDialog = false },
            onDeleted = {
                navController.navigate("login") {
                    popUpTo(0) { inclusive = true }
                }
            }
        )
    }
}

@Composable
fun ProfileSection(navController: NavHostController) {
    val user = remember { MockServer.getCurrentUser() }
    val settings = remember { MockServer.getCurrentUserSettings() }

    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(16.dp)
            .clickable { navController.navigate("settings_edit_profile") },
        verticalAlignment = Alignment.CenterVertically
    ) {
        // Avatar
        Surface(
            shape = CircleShape,
            color = MaterialTheme.colorScheme.primaryContainer,
            modifier = Modifier.size(80.dp)
        ) {
            Box(contentAlignment = Alignment.Center) {
                if (settings?.avatarUrl?.isNotEmpty() == true) {
                    // Load image with Coil
                    Text("📷")
                } else {
                    Text(
                        text = user?.name?.first()?.toString() ?: "J",
                        style = MaterialTheme.typography.headlineLarge
                    )
                }
            }
        }

        Spacer(modifier = Modifier.width(16.dp))

        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = user?.name ?: "Jatin",
                style = MaterialTheme.typography.headlineSmall
            )
            Text(
                text = "@${settings?.username ?: "jatin"}",
                style = MaterialTheme.typography.bodyLarge,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
            Text(
                text = user?.email ?: "",
                style = MaterialTheme.typography.bodyMedium,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }

        Icon(
            imageVector = Icons.Default.ChevronRight,
            contentDescription = "Edit",
            tint = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

@Composable
fun SettingsCategory(
    title: String,
    color: Color = MaterialTheme.colorScheme.onSurface,
    content: @Composable ColumnScope.() -> Unit
) {
    Column {
        Text(
            text = title,
            style = MaterialTheme.typography.labelLarge,
            color = color,
            modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
        )
        Column(content = content)
    }
}

@Composable
fun SettingsItem(
    icon: androidx.compose.ui.graphics.vector.ImageVector,
    title: String,
    subtitle: String = "",
    titleColor: Color = MaterialTheme.colorScheme.onSurface,
    trailing: @Composable (() -> Unit)? = null,
    onClick: () -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(horizontal = 16.dp, vertical = 12.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Icon(
            imageVector = icon,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.size(24.dp)
        )

        Spacer(modifier = Modifier.width(16.dp))

        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = title,
                style = MaterialTheme.typography.bodyLarge,
                color = titleColor
            )
            if (subtitle.isNotEmpty()) {
                Text(
                    text = subtitle,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
        }

        trailing?.invoke()
    }
}

@Composable
fun DeleteAccountDialog(onDismiss: () -> Unit, onDeleted: () -> Unit) {
    var password by remember { mutableStateOf("") }
    var error by remember { mutableStateOf("") }
    var isDeleting by remember { mutableStateOf(false) }
    var showConfirm by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text("Delete Account", color = MaterialTheme.colorScheme.error) },
        text = {
            Column {
                if (!showConfirm) {
                    Text(
                        "This will permanently delete your account and all data. This action cannot be undone.",
                        color = MaterialTheme.colorScheme.error
                    )
                    Spacer(modifier = Modifier.height(16.dp))
                    Text("To confirm, type your password:")
                    Spacer(modifier = Modifier.height(8.dp))
                    OutlinedTextField(
                        value = password,
                        onValueChange = { password = it; error = "" },
                        label = { Text("Current Password") },
                        visualTransformation = PasswordVisualTransformation(),
                        isError = error.isNotEmpty(),
                        supportingText = { if (error.isNotEmpty()) Text(error) }
                    )
                } else {
                    Text("⚠️ Final warning: All your posts, messages, friends, and data will be permanently deleted.")
                }
            }
        },
        confirmButton = {
            TextButton(
                onClick = {
                    if (!showConfirm) {
                        if (password.isBlank()) {
                            error = "Password is required"
                            return@TextButton
                        }
                        showConfirm = true
                    } else {
                        isDeleting = true
                        scope.launch {
                            delay(1000)
                            val result = MockServer.deleteAccount(password)
                            isDeleting = false
                            result.onSuccess {
                                onDeleted()
                            }.onFailure {
                                error = it.message ?: "Failed to delete account"
                                showConfirm = false
                            }
                        }
                    }
                },
                colors = ButtonDefaults.textButtonColors(contentColor = MaterialTheme.colorScheme.error),
                enabled = !isDeleting
            ) {
                if (isDeleting) {
                    CircularProgressIndicator(modifier = Modifier.size(20.dp))
                } else {
                    Text(if (!showConfirm) "Continue" else "Permanently Delete")
                }
            }
        },
        dismissButton = {
            TextButton(onClick = onDismiss) {
                Text("Cancel")
            }
        }
    )
}