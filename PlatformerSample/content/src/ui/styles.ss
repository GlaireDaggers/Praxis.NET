* {
	font: content/font/RussoOne-Regular.ttf;
	size: 32;
}

#scoretext {
    wordWrap: false;
    horizontalAlign: center;
}

ButtonWidget {
    image: content/image/blue_panel.dds;
    slices: 10, 10, 10, 10;
    imageColor: 190, 190, 190, 255;
    textColor: 255, 255, 255, 255;
    horizontalAlign: center;
    verticalAlign: center;
}

ButtonWidget:hover {
    imageColor: 255, 255, 255, 255;
    textColor: 255, 255, 0, 255;
}

ButtonWidget:press {
    imageColor: 128, 128, 128, 255;
}
