package com.jatin.runapp.screens

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.PersonAdd
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import com.jatin.runapp.data.MockServer
import com.jatin.runapp.data.Post

@Composable
fun HomeScreen(navController: NavHostController) {
    var posts by remember { mutableStateOf(MockServer.getFeed()) }
    var suggestedUsers by remember { mutableStateOf(MockServer.getSuggestedUsers()) }

    LazyColumn(
        modifier = Modifier.fillMaxSize(),
        contentPadding = PaddingValues(vertical = 8.dp)
    ) {
        // Suggested users to follow
        if (suggestedUsers.isNotEmpty()) {
            item {
                Text(
                    text = "People you may know",
                    style = MaterialTheme.typography.titleMedium,
                    modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)
                )
                
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 12.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    suggestedUsers.take(3).forEach { user ->
                        SuggestedUserCard(user, navController) {
                            MockServer.followUser(user.id)
                            suggestedUsers = MockServer.getSuggestedUsers()
                        }
                    }
                }
                
                Spacer(modifier = Modifier.height(8.dp))
                Divider(modifier = Modifier.padding(horizontal = 16.dp))
            }
        }

        // Posts
        items(posts) { post ->
            PostCard(post, navController) {
                posts = posts.map { 
                    if (it.id == post.id) MockServer.likePost(post.id) ?: it else it 
                }
            }
        }
    }
}

@Composable
fun SuggestedUserCard(
    user: com.jatin.runapp.data.User,
    navController: NavHostController,
    onFollow: () -> Unit
) {
    Card(
        modifier = Modifier.width(120.dp),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier.padding(12.dp)
        ) {
            Surface(
                shape = MaterialTheme.shapes.small,
                color = MaterialTheme.colorScheme.primaryContainer,
                modifier = Modifier.size(50.dp)
            ) {
                Box(contentAlignment = Alignment.Center) {
                    Text(
                        text = user.name.first().toString(),
                        style = MaterialTheme.typography.titleLarge
                    )
                }
            }
            
            Spacer(modifier = Modifier.height(8.dp))
            
            Text(
                text = user.name,
                style = MaterialTheme.typography.bodyMedium,
                maxLines = 1
            )
            
            Spacer(modifier = Modifier.height(4.dp))
            
            Button(
                onClick = onFollow,
                modifier = Modifier.height(32.dp),
                contentPadding = PaddingValues(horizontal = 12.dp)
            ) {
                Icon(
                    Icons.Default.PersonAdd,
                    contentDescription = "Follow",
                    modifier = Modifier.size(16.dp)
                )
                Spacer(modifier = Modifier.width(4.dp))
                Text("Follow", style = MaterialTheme.typography.labelSmall)
            }
        }
    }
}

@Composable
fun PostCard(
    post: Post,
    navController: NavHostController,
    onLike: () -> Unit
) {
    Card(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 12.dp, vertical = 6.dp),
        elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
    ) {
        Column(modifier = Modifier.padding(16.dp)) {
            // Author info - clickable
            Row(
                verticalAlignment = Alignment.CenterVertically,
                modifier = Modifier
                    .fillMaxWidth()
                    .clickable {
                        navController.navigate("user_profile/${post.authorId}")
                    }
            ) {
                Surface(
                    shape = MaterialTheme.shapes.small,
                    color = MaterialTheme.colorScheme.primaryContainer,
                    modifier = Modifier.size(40.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Text(
                            text = post.authorName.first().toString(),
                            style = MaterialTheme.typography.titleMedium
                        )
                    }
                }

                Spacer(modifier = Modifier.width(12.dp))

                Column {
                    Text(
                        text = post.authorName,
                        style = MaterialTheme.typography.titleMedium
                    )
                    Text(
                        text = post.timestamp,
                        style = MaterialTheme.typography.bodySmall,
                        color = MaterialTheme.colorScheme.onSurfaceVariant
                    )
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            // Post content
            Text(
                text = post.content,
                style = MaterialTheme.typography.bodyLarge
            )

            // Image placeholder
            if (post.imageUrl != null) {
                Spacer(modifier = Modifier.height(12.dp))
                Surface(
                    color = MaterialTheme.colorScheme.surfaceVariant,
                    modifier = Modifier
                        .fillMaxWidth()
                        .height(200.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Text("📷 Image", style = MaterialTheme.typography.bodyLarge)
                    }
                }
            }

            Spacer(modifier = Modifier.height(12.dp))

            Divider()

            Spacer(modifier = Modifier.height(8.dp))

            // Like and Comment buttons
            Row(
                modifier = Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceEvenly
            ) {
                TextButton(onClick = onLike) {
                    Text(
                        text = if (post.isLiked) "❤️ ${post.likes}" else "🤍 ${post.likes}",
                        color = if (post.isLiked) MaterialTheme.colorScheme.error else MaterialTheme.colorScheme.onSurface
                    )
                }

                TextButton(onClick = { }) {
                    Text("💬 ${post.comments}")
                }

                TextButton(onClick = { }) {
                    Text("↗️ ${post.shares}")
                }
            }
        }
    }
}