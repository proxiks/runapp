#include <QApplication>
#include <QStyleFactory>
#include "ui/mainwindow.h"

int main(int argc, char *argv[]) {
    QApplication app(argc, argv);
    
    // Dark theme like Facebook
    app.setStyle(QStyleFactory::create("Fusion"));
    QPalette darkPalette;
    darkPalette.setColor(QPalette::Window, QColor(24, 25, 26));
    darkPalette.setColor(QPalette::WindowText, Qt::white);
    darkPalette.setColor(QPalette::Base, QColor(36, 37, 38));
    darkPalette.setColor(QPalette::AlternateBase, QColor(24, 25, 26));
    darkPalette.setColor(QPalette::Text, Qt::white);
    darkPalette.setColor(QPalette::Button, QColor(58, 59, 60));
    darkPalette.setColor(QPalette::ButtonText, Qt::white);
    darkPalette.setColor(QPalette::Highlight, QColor(24, 119, 242));
    app.setPalette(darkPalette);
    
    app.setFont(QFont("Inter", 10));
    
    MainWindow window;
    window.show();
    
    return app.exec();
}