/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("rimraf"),
    concat = require("gulp-concat"),
    cssmin = require("gulp-cssmin"),
    uglify = require("gulp-uglify");

var paths = {
    webroot: "./wwwroot/"
};

paths.js = paths.webroot + "js/**/*.js";
paths.minJs = paths.webroot + "js/**/*.min.js";
paths.skyJs = paths.webroot + "js/sky-forms-ie8.js";
paths.css = paths.webroot + "css/**/*.css";
paths.skyCss = paths.webroot + "css/**/sky-forms.css";
paths.minCss = paths.webroot + "css/**/*.min.css";
paths.concatJsDest = paths.webroot + "js/site.min.js";
paths.concatCssDest = paths.webroot + "css/site.min.css";
paths.concatSkyCssDest = paths.webroot + "css/sky-forms.min.css";

gulp.task("clean:js", function (cb) {
    rimraf(paths.concatJsDest, cb);
});

gulp.task("clean:css", function (cb) {
    rimraf(paths.concatCssDest, cb);
});

gulp.task("clean:skycss", function (cb) {
    rimraf(paths.concatSkyCssDest, cb);
});

gulp.task("clean", ["clean:js", "clean:css", "clean:skycss"]);

gulp.task("min:js", function () {
    return gulp.src([paths.js, "!" + paths.skyJs, "!" + paths.minJs], { base: "." })
        .pipe(concat(paths.concatJsDest))
        .pipe(uglify())
        .pipe(gulp.dest("."));
});

gulp.task("min:skycss", function () {
    return gulp.src([paths.skyCss, "!" + paths.minCss])
        .pipe(concat(paths.concatSkyCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min:css", function () {
    return gulp.src([paths.css, "!" + paths.skyCss, "!" + paths.minCss])
        .pipe(concat(paths.concatCssDest))
        .pipe(cssmin())
        .pipe(gulp.dest("."));
});

gulp.task("min", ["min:js", "min:css", "min:skycss"]);
