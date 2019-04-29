var gulp = require('gulp'),
    autoprefixer = require('gulp-autoprefixer'),
    livereload = require('gulp-livereload'),
    notify = require('gulp-notify'),
    sass = require('gulp-sass'),
    gulpif = require('gulp-if'),
    sassLint = require('gulp-sass-lint'),
    serve = require('gulp-serve'),
    sourcemaps = require('gulp-sourcemaps'),
    exec = require('child_process').exec;

var debug = false;
var assets = 'templates/uno';
/**
 * Put relative path to your assets :
 * - Wordpress  : 'public/wp-content/themes/YOUR_THEME/assets';
 * - Symfony : 'public/assets/';
 */

gulp.task('styles', function() {
    var output = debug ? 'nested' : 'compressed';
    return gulp
        .src(assets + '/styles/scss/main.scss')
        .pipe(gulpif(debug, sourcemaps.init()))
        .pipe(gulpif(debug, sassLint()))
        .pipe(gulpif(debug, sassLint.format()))
        .pipe(gulpif(debug, sassLint.failOnError()))
        .pipe(
            sass({ includePaths: ['./node_modules/'], outputStyle: output }).on(
                'error',
                sass.logError
            )
        )
        .pipe(autoprefixer({ browsers: ['last 2 versions', '> 5%'] }))
        .pipe(gulpif(debug, sourcemaps.write()))
        .pipe(gulp.dest(assets + '/styles'))
        .pipe(livereload())
        .pipe(notify({ message: 'CSS complete' }));
});

gulp.task('watch', function() {
    livereload.listen();

    gulp.watch(
        [assets + '/styles/scss/**/*.scss', assets + '/styles/scss/**/*.sass'],
        ['styles', 'build']
    ).on('change', function(event) {
        console.log(
            'File ' +
                event.path +
                ' was ' +
                event.type +
                ', running CSS task...'
        );
    });
});

gulp.task('default', function() {
    gulp.start('styles', 'watch', 'build', 'serve');
});

gulp.task('debug', function() {
    debug = true;
    gulp.start('styles');
});

gulp.task('serve', serve('_site'));

gulp.task('build', function(cb) {
    exec('docfx build', function(err, stdout, stderr) {
        console.log(stdout);
        console.log(stderr);
        cb(err);
    });
});

/**
 * Handle errors and displays them in console
 * @param error
 */
function swallowError(error) {
    console.log(error.toString());
    this.emit('end');
}
