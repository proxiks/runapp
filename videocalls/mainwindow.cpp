#include "mainwindow.h"
#include "loginwidget.h"
#include "feedwidget.h"
#include "reelplayer.h"
#include <QVBoxLayout>

MainWindow::MainWindow(QWidget *parent) : QMainWindow(parent) {
    setupUI();
    setWindowTitle("RunApp");
    setMinimumSize(900, 600);
    resize(1200, 800);
}

MainWindow::~MainWindow() = default;

void MainWindow::setupUI() {
    auto* central = new QWidget(this);
    setCentralWidget(central);
    
    auto* layout = new QVBoxLayout(central);
    layout->setContentsMargins(0, 0, 0, 0);
    
    stack = new QStackedWidget(this);
    
    loginPage = new LoginWidget(this);
    feedPage = new FeedWidget(this);
    reelPlayer = new ReelPlayer(this);
    
    stack->addWidget(loginPage);
    stack->addWidget(feedPage);
    stack->addWidget(reelPlayer);
    
    layout->addWidget(stack);
    
    connect(loginPage, &LoginWidget::loginSuccess, 
            this, &MainWindow::onLoginSuccess);
    connect(feedPage, &FeedWidget::showReels,
            this, &MainWindow::showReels);
    connect(reelPlayer, &ReelPlayer::backToFeed,
            this, [this]() { stack->setCurrentWidget(feedPage); });
    
    // Check saved session
    // if (hasValidCookie()) showFeed();
}

void MainWindow::onLoginSuccess(const QString& userId, const QString& token) {
    feedPage->setUser(userId, token);
    stack->setCurrentWidget(feedPage);
}

void MainWindow::onLogout() {
    // Clear cookies, reset state
    stack->setCurrentWidget(loginPage);
}

void MainWindow::showReels() {
    reelPlayer->loadReels();
    stack->setCurrentWidget(reelPlayer);
}