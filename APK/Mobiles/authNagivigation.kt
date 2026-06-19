package com.jatin.jatinbook.screens

import androidx.compose.runtime.*
import androidx.navigation.compose.*

@Composable
fun AuthNavigation(onLoginSuccess: () -> Unit) {
    val navController = rememberNavController()

    NavHost(navController = navController, startDestination = "login") {
        composable("login") {
            LoginScreen(
                navController = navController,
                onLoginSuccess = onLoginSuccess
            )
        }
        composable("signup") {
            SignUpScreen(navController = navController)
        }
    }
}