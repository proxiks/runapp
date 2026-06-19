package com.jatin.runapp.screens

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.AddPhotoAlternate
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import com.jatin.runapp.data.MockServer
import kotlinx.coroutines.launch

@Composable
fun CreatePostScreen() {
    var postText by remember { mutableStateOf("") }
    var showSuccess by remember { mutableStateOf(false) }
    var isPosting by remember { mutableStateOf(false) }
    val scope = rememberCoroutineScope()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
    ) {
        if (showSuccess) {
            Card(
                colors = CardDefaults.cardColors(
                    containerColor = MaterialTheme.colorScheme.primaryContainer
                ),
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(bottom = 16.dp)
            ) {
                Text(
                    text = "✅ Post created successfully!",
                    modifier = Modifier.padding(16.dp),
                    style = MaterialTheme.typography.bodyLarge
                )
            }
        }

        OutlinedTextField(
            value = postText,
            onValueChange = { postText = it },
            label = { Text("What's on your mind?") },
            modifier = Modifier
                .fillMaxWidth()
                .weight(1f),
            maxLines = 10
        )

        Spacer(modifier = Modifier.height(16.dp))

        OutlinedButton(
            onClick = { },
            modifier = Modifier.fillMaxWidth()
        ) {
            Icon(
                imageVector = Icons.Default.AddPhotoAlternate,
                contentDescription = "Add Photo"
            )
            Spacer(modifier = Modifier.width(8.dp))
            Text("Add Photo/Video")
        }

        Spacer(modifier = Modifier.height(16.dp))

        Button(
            onClick = {
                if (postText.isNotBlank()) {
                    isPosting = true
                    scope.launch {
                        MockServer.createPost(postText)
                        isPosting = false
                        showSuccess = true
                        postText = ""
                        kotlinx.coroutines.delay(2000)
                        showSuccess = false
                    }
                }
            },
            modifier = Modifier
                .fillMaxWidth()
                .height(48.dp),
            enabled = !isPosting && postText.isNotBlank()
        ) {
            if (isPosting) {
                CircularProgressIndicator(
                    modifier = Modifier.size(24.dp),
                    color = MaterialTheme.colorScheme.onPrimary
                )
            } else {
                Text("Post")
            }
        }
    }
    OutlinedButton(
    onClick = { navController.navigate("meme_generator") },
    modifier = Modifier.fillMaxWidth()
) {
    Icon(Icons.Default.AutoAwesome, null)
    Spacer(modifier = Modifier.width(8.dp))
    Text("🤖 AI Meme Generator")
}
}