package com.jatin.runapp.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.NavHostController
import com.jatin.runapp.data.MockServer
import com.jatin.runapp.data.User

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun UserProfileScreen(navController: NavHostController, userId: String) {
    val user = remember { MockServer.getSuggestedUsers().find { it.id == userId } }
    var isFollowing by remember { mutableStateOf(user?.isFollowing ?: false) }
    var isFriend by remember { mutableStateOf(user?.isFriend ?: false) }
    var friendRequestSent by remember { mutableStateOf(user?.friendRequestSent ?: false) }

    if (user == null) {
        Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
            Text("User not found")
        }
        return
    }

    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text(user.name) },
                navigationIcon = {
                    IconButton(onClick = { navController.popBackStack() }) {
                        Icon(Icons.AutoMirrored.Filled.ArrowBack, contentDescription = "Back")
                    }
                }
            )
        }
    ) { padding ->
        LazyColumn(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            item {
                Spacer(modifier = Modifier.height(24.dp))

                Surface(
                    shape = MaterialTheme.shapes.extraLarge,
                    color = MaterialTheme.colorScheme.primaryContainer,
                    modifier = Modifier.size(120.dp)
                ) {
                    Box(contentAlignment = Alignment.Center) {
                        Text(
                            text = user.name.first().toString(),
                            style = MaterialTheme.typography.headlineLarge
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                Text(
                    text = user.name,
                    style = MaterialTheme.typography.headlineMedium
                )

                Text(
                    text = user.bio,
                    style = MaterialTheme.typography.bodyLarge,
                    color = MaterialTheme.colorScheme.onSurfaceVariant
                )

                Spacer(modifier = Modifier.height(16.dp))

                // Stats
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceEvenly
                ) {
                    StatDisplay(user.followersCount.toString(), "Followers")
                    StatDisplay(user.followingCount.toString(), "Following")
                    StatDisplay(user.friendsCount.toString(), "Friends")
                }

                Spacer(modifier = Modifier.height(24.dp))

                // Action buttons
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp),
                    horizontalArrangement = Arrangement.spacedBy(8.dp)
                ) {
                    if (isFriend) {
                        OutlinedButton(
                            onClick = { },
                            modifier = Modifier.weight(1f)
                        ) {
                            Text("Friends ✓")
                        }
                    } else if (friendRequestSent) {
                        OutlinedButton(
                            onClick = { },
                            modifier = Modifier.weight(1f),
                            enabled = false
                        ) {
                            Text("Request Sent")
                        }
                    } else {
                        Button(
                            onClick = {
                                MockServer.sendFriendRequest(userId)
                                friendRequestSent = true
                            },
                            modifier = Modifier.weight(1f)
                        ) {
                            Icon(Icons.Default.PersonAdd, contentDescription = null)
                            Spacer(modifier = Modifier.width(4.dp))
                            Text("Add Friend")
                        }
                    }

                    if (isFollowing) {
                        OutlinedButton(
                            onClick = {
                                MockServer.unfollowUser(userId)
                                isFollowing = false
                            },
                            modifier = Modifier.weight(1f)
                        ) {
                            Text("Following")
                        }
                    } else {
                        Button(
                            onClick = {
                                MockServer.followUser(userId)
                                isFollowing = true
                            },
                            modifier = Modifier.weight(1f)
                        ) {
                            Text("Follow")
                        }
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))
                Divider()
                Spacer(modifier = Modifier.height(16.dp))

                Text(
                    text = "Posts",
                    style = MaterialTheme.typography.titleLarge,
                    modifier = Modifier.padding(horizontal = 16.dp)
                )
            }
    
// In ProfileScreen, add this to the top bar actions or in the content:

// Add after the stats row in ProfileScreen:
Row(
    modifier = Modifier
        .fillMaxWidth()
        .padding(horizontal = 16.dp, vertical = 8.dp),
    horizontalArrangement = Arrangement.spacedBy(8.dp)
) {
    OutlinedButton(
        onClick = { navController.navigate("settings_edit_profile") },
        modifier = Modifier.weight(1f)
    ) {
        Icon(Icons.Default.Edit, contentDescription = null, modifier = Modifier.size(18.dp))
        Spacer(modifier = Modifier.width(4.dp))
        Text("Edit Profile")
    }
    
    OutlinedButton(
        onClick = { navController.navigate("settings") },
        modifier = Modifier.weight(1f)
    ) {
        Icon(Icons.Default.Settings, contentDescription = null, modifier = Modifier.size(18.dp))
        Spacer(modifier = Modifier.width(4.dp))
        Text("Settings")
    }
}

            // User's posts
            val userPosts = MockServer.getFeed().filter { it.authorId == userId }
            items(userPosts.size) { index ->
                val post = userPosts[index]
                Card(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(horizontal = 16.dp, vertical = 4.dp)
                ) {
                    Column(modifier = Modifier.padding(16.dp)) {
                        Text(
                            text = post.content,
                            style = MaterialTheme.typography.bodyLarge
                        )
                        Spacer(modifier = Modifier.height(8.dp))
                        Text(
                            text = "❤️ ${post.likes}  💬 ${post.comments}  •  ${post.timestamp}",
                            style = MaterialTheme.typography.bodySmall,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
            }
        }
    }
}

@Composable
fun StatDisplay(value: String, label: String) {
    Column(horizontalAlignment = Alignment.CenterHorizontally) {
        Text(
            text = value,
            style = MaterialTheme.typography.titleLarge
        )
        Text(
            text = label,
            style = MaterialTheme.typography.bodyMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}