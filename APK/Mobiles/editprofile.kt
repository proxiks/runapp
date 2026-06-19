package com.jatin.runapp.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.CameraAlt
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import com.jatin.runapp.data.MockServer
import com.jatin.runapp.data.UserSettings
import com.jatin.runapp.utils.ValidationUtils
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun EditProfileScreen(navController: NavHostController) {
    val currentSettings = remember { MockServer.getCurrentUserSettings() }
    
    var name by remember { mutableStateOf(currentSettings?.name ?: "") }
    var username by remember { mutableStateOf(currentSettings?.username ?: "") }
    var bio by remember { mutableStateOf(currentSettings?.bio ?: "") }
    
    var nameError by remember { mutableStateOf("") }
    var usernameError by remember { mutableStateOf("") }
    var bioError by remember { mutableStateOf("") }
    
    var isSaving by remember { mutableStateOf(false) }
    var showSuccess by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Edit Profile") },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                },
                actions = {
                    IconButton(
                        onClick = {
                            // Validate all
                            val nameVal = ValidationUtils.validateName(name)
                            val usernameVal = ValidationUtils.validateUsername(username)
                            val bioVal = ValidationUtils.validateBio(bio)
                            
                            nameError = nameVal.errorMessage ?: ""
                            usernameError = usernameVal.errorMessage ?: ""
                            bioError = bioVal.errorMessage ?: ""
                            
                            if (nameVal.isValid && usernameVal.isValid && bioVal.isValid) {
                                isSaving = true
                                scope.launch {
                                    val result = MockServer.updateProfile(name, username, bio)
                                    isSaving = false
                                    result.onSuccess {
                                        showSuccess = true
                                    }.onFailure {
                                        // Show error
                                    }
                                }
                            }
                        },
                        enabled = !isSaving
                    ) {
                        if (isSaving) {
                            CircularProgressIndicator(modifier = Modifier.size(24.dp))
                        } else {
                            Icon(Icons.Default.Check, contentDescription = "Save")
                        }
                    }
                }
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .verticalScroll(rememberScrollState()),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            if (showSuccess) {
                Card(
                    colors = CardDefaults.cardColors(
                        containerColor = MaterialTheme.colorScheme.primaryContainer
                    ),
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(16.dp)
                ) {
                    Text(
                        text = "✅ Profile updated successfully!",
                        modifier = Modifier.padding(16.dp),
                        style = MaterialTheme.typography.bodyLarge
                    )
                }
            }

            // Avatar with edit button
            Box(
                modifier = Modifier.padding(vertical = 24.dp),
                contentAlignment = Alignment.BottomEnd
            ) {
                Surface(
                    shape = CircleShape,
                    color = MaterialTheme.colorScheme.primaryContainer,
                    modifier = Modifier.size(120.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Text(
                            text = name.firstOrNull()?.toString() ?: "J",
                            style = MaterialTheme.typography.headlineLarge
                        )
                    }
                }
                
                FloatingActionButton(
                    onClick = { /* Open image picker */ },
                    modifier = Modifier.size(40.dp),
                    containerColor = MaterialTheme.colorScheme.primary
                ) {
                    Icon(
                        Icons.Default.CameraAlt,
                        contentDescription = "Change Photo",
                        modifier = Modifier.size(20.dp)
                    )
                }
            }

            // Name field
            OutlinedTextField(
                value = name,
                onValueChange = { 
                    name = it
                    nameError = ValidationUtils.validateName(it).errorMessage ?: ""
                },
                label = { Text("Name") },
                isError = nameError.isNotEmpty(),
                supportingText = { 
                    if (nameError.isNotEmpty()) Text(nameError, color = MaterialTheme.colorScheme.error)
                    else Text("${name.length}/50")
                },
                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp)
            )

            Spacer(modifier = Modifier.height(16.dp))

            // Username field
            OutlinedTextField(
                value = username,
                onValueChange = { 
                    username = it.lowercase()
                    usernameError = ValidationUtils.validateUsername(it).errorMessage ?: ""
                },
                label = { Text("Username") },
                prefix = { Text("@") },
                isError = usernameError.isNotEmpty(),
                supportingText = { 
                    if (usernameError.isNotEmpty()) Text(usernameError, color = MaterialTheme.colorScheme.error)
                    else Text("runapp.com/@$username")
                },
                keyboardOptions = KeyboardOptions(
                    keyboardType = KeyboardType.Ascii,
                    imeAction = ImeAction.Next
                ),
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp)
            )

            Spacer(modifier = Modifier.height(16.dp))

            // Bio field
            OutlinedTextField(
                value = bio,
                onValueChange = { 
                    bio = it
                    bioError = ValidationUtils.validateBio(it).errorMessage ?: ""
                },
                label = { Text("Bio") },
                isError = bioError.isNotEmpty(),
                supportingText = { 
                    if (bioError.isNotEmpty()) Text(bioError, color = MaterialTheme.colorScheme.error)
                    else Text("${bio.length}/500")
                },
                minLines = 3,
                maxLines = 5,
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(horizontal = 16.dp)
            )
        }
    }
}