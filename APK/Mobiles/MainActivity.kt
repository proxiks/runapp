package com.jatin.runapp

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.navigation.NavDestination.Companion.hierarchy
import androidx.navigation.NavGraph.Companion.findStartDestination
import androidx.navigation.compose.*
import com.jatin.runapp.data.MockServer
import com.jatin.runapp.ui.theme.JatinBookTheme
import com.jatin.runapp.screens.*

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            JatinBookTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    MainApp()
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainApp() {
    val navController = rememberNavController()
    val navBackStackEntry by navController.currentBackStackEntryAsState()
    val currentDestination = navBackStackEntry?.destination
    
    var isLoggedIn by remember { mutableStateOf(false) }
    var unreadCount by remember { mutableIntStateOf(0) }
    
    // Refresh unread count periodically
    LaunchedEffect(isLoggedIn) {
        if (isLoggedIn) {
            while (true) {
                unreadCount = MockServer.getUnreadCount()
                delay(5000)
            }
        }
    }

    if (!isLoggedIn) {
        AuthNavigation(
            onLoginSuccess = { 
                isLoggedIn = true
                unreadCount = MockServer.getUnreadCount()
            }
        )
    } else {
        Scaffold(
            topBar = {
                if (currentDestination?.route != "notifications") {
                    TopAppBar(
                        title = { Text("runapp") },
                        actions = {
                            BadgedBox(
                                badge = {
                                    if (unreadCount > 0) {
                                        Badge { Text(unreadCount.toString()) }
                                    }
                                }
                            ) {
                                IconButton(onClick = { 
                                    navController.navigate("notifications")
                                    unreadCount = 0
                                }) {
                                    Icon(Icons.Default.Notifications, contentDescription = "Notifications")
                                }
                            }
                        }
                    )
                }
            },
            bottomBar = {
                NavigationBar {
                    val items = listOf(
                        Screen.Home,
                        Screen.Videos,
                        Screen.Create,
                        Screen.Profile
                    )
                    items.forEach { screen ->
                        NavigationBarItem(
                            icon = { Icon(screen.icon, contentDescription = screen.label) },
                            label = { Text(screen.label) },
                            selected = currentDestination?.hierarchy?.any { it.route == screen.route } == true,
                            onClick = {
                                navController.navigate(screen.route) {
                                    popUpTo(navController.graph.findStartDestination().id) {
                                        saveState = true
                                    }
                                    launchSingleTop = true
                                    restoreState = true
                                }
                            }
                        )
                    }
                }
            }
        ) { innerPadding ->
            NavHost(
                navController = navController,
                startDestination = Screen.Home.route,
                modifier = Modifier.padding(innerPadding)
            ) {
                composable(Screen.Home.route) { HomeScreen(navController) }
                composable(Screen.Videos.route) { VideosScreen(navController) }
                composable(Screen.Create.route) { CreatePostScreen() }
                composable(Screen.Profile.route) { ProfileScreen(navController) }
                composable("notifications") { NotificationsScreen(navController) }
                composable("user_profile/{userId}") { backStackEntry ->
                    val userId = backStackEntry.arguments?.getString("userId") ?: ""
                    UserProfileScreen(navController, userId)
                }
                composable("friends") { FriendsScreen(navController) }
                composable("friend_requests") { FriendRequestsScreen(navController) }
                composable("settings") { SettingsScreen(navController) }
composable("settings_edit_profile") { EditProfileScreen(navController) }
composable("settings_email") { EmailSettingsScreen(navController) }
composable("settings_phone") { PhoneSettingsScreen(navController) }
composable("settings_password") { PasswordSettingsScreen(navController) }
composable("settings_2fa") { TwoFAScreen(navController) }
composable("settings_privacy") { PrivacySettingsScreen(navController) }
composable("meme_generator") { MemeGeneratorScreen(navController) }
            }
        }
    }
}

sealed class Screen(val route: String, val label: String, val icon: androidx.compose.ui.graphics.vector.ImageVector) {
    object Home : Screen("home", "Home", Icons.Default.Home)
    object Videos : Screen("videos", "Videos", Icons.Default.PlayArrow)
    object Create : Screen("create", "Create", Icons.Default.Add)
    object Profile : Screen("profile", "Profile", Icons.Default.Person)
}