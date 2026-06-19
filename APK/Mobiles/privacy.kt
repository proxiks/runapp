package com.jatin.runapp.screens

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import com.jatin.runapp.data.MockServer
import com.jatin.runapp.data.PrivacyLevel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PrivacySettingsScreen(navController: NavHostController) {
    val settings = remember { MockServer.getCurrentUserSettings() }
    var selectedPrivacy by remember { mutableStateOf(settings?.privacyLevel ?: PrivacyLevel.PUBLIC) }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Privacy") },
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
            Text(
                text = "Who can see your profile?",
                style = MaterialTheme.typography.titleLarge,
                modifier = Modifier.padding(bottom = 16.dp)
            )

            PrivacyOption(
                title = "Public",
                description = "Everyone can see your profile and posts",
                selected = selectedPrivacy == PrivacyLevel.PUBLIC,
                onSelect = {
                    selectedPrivacy = PrivacyLevel.PUBLIC
                    MockServer.updatePrivacyLevel(PrivacyLevel.PUBLIC)
                }
            )

            Spacer(modifier = Modifier.height(12.dp))

            PrivacyOption(
                title = "Friends Only",
                description = "Only your friends can see your profile and posts",
                selected = selectedPrivacy == PrivacyLevel.FRIENDS,
                onSelect = {
                    selectedPrivacy = PrivacyLevel.FRIENDS
                    MockServer.updatePrivacyLevel(PrivacyLevel.FRIENDS)
                }
            )

            Spacer(modifier = Modifier.height(12.dp))

            PrivacyOption(
                title = "Private",
                description = "Only you can see your profile and posts",
                selected = selectedPrivacy == PrivacyLevel.PRIVATE,
                onSelect = {
                    selectedPrivacy = PrivacyLevel.PRIVATE
                    MockServer.updatePrivacyLevel(PrivacyLevel.PRIVATE)
                }
            )
        }
    }
}

@Composable
fun PrivacyOption(
    title: String,
    description: String,
    selected: Boolean,
    onSelect: () -> Unit
) {
    Card(
        modifier = Modifier.fillMaxWidth(),
        colors = CardDefaults.cardColors(
            containerColor = if (selected) 
                MaterialTheme.colorScheme.primaryContainer 
            else 
                MaterialTheme.colorScheme.surface
        ),
        onClick = onSelect
    ) {
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(16.dp),
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column(modifier = Modifier.weight(1f)) {
                Text(
                    text = title,
                    style = MaterialTheme.typography.titleMedium
                )
                Text(
                    text = description,
                    style = MaterialTheme.typography.bodyMedium,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )
            }
            RadioButton(
                selected = selected,
                onClick = onSelect
            )
        }
    }
}