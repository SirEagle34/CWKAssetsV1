# Standard library imports
import argparse
import base64
import gzip
import hmac
import hashlib
import json
import logging
import os
import random
import re
import secrets
import smtplib
import shutil
import string
import subprocess
import threading
import traceback
import time
from email.message import EmailMessage
from zoneinfo import ZoneInfo
from datetime import datetime, timedelta, timezone
from io import BytesIO
from urllib.parse import parse_qs
from typing import Optional

# Third-party library imports
import schedule

from secret import (
    SMTP_HOST,
    SMTP_PORT,
    SMTP_USERNAME,
    SMTP_PASSWORD,
    SMTP_FROM
)

from flask import (Flask, Request, abort, jsonify, make_response, redirect,
                   render_template, request, send_from_directory, session)
from flask_bcrypt import Bcrypt
from flask_login import (LoginManager, UserMixin, current_user,
                         login_required, login_user, logout_user)
from flask_sqlalchemy import SQLAlchemy
from sqlalchemy.orm import DeclarativeBase, Mapped, mapped_column
from sqlalchemy.sql import func

parser = argparse.ArgumentParser()
parser.add_argument('--port', type=int, default=5000)
parser.add_argument('--debug', action='store_true')

args, _ = parser.parse_known_args()

app = Flask(__name__)

#check if flaskkey exists
if not os.path.exists("flaskkey"):
	print("Creating new Flask secret key")
	#create a new key
	with open("flaskkey", "w") as f:
		f.write(''.join(random.choice(string.ascii_letters + string.digits) for i in range(50)))
app.secret_key = open("flaskkey", "r").read()

bcrypt = Bcrypt(app)

login_manager = LoginManager(app)
login_manager.login_view = '/'


class Base(DeclarativeBase):
  pass
app.config["SQLALCHEMY_DATABASE_URI"] = "sqlite:///cardwarskingdom.db"
db = SQLAlchemy(model_class=Base)
db.init_app(app)

badcharaters = ['/', '\\', ':', '*', '?', '"', '<', '>', '|', ";", "%", "^", "&", "(", ")", "{", "}", "[", "]", ".", ",", "'", "`", "!", "$", "#", "@", "+", "="]

maintenance = False

@app.route("/static/version.txt")
def PersistVersion():
	with open("data/persist/version.txt", "r") as f:
		pc_version = f.read()
	with open("data/persist/android_version.txt", "r") as f:
		android_version = f.read()

	data = {
		"maintenance_mode": "yes" if maintenance else "no",
		"message": "Card Wars Kingdom is currently undergoing maintenance.\n\nPlease try again later.",
		"icon": "",
		"clickable": "yes",
		"android_version": android_version,
		"version": pc_version,
		"android_url": "https://github.com/shishkabob27/CardWarsKingdom/releases",
		"pc_url": "https://github.com/shishkabob27/CardWarsKingdom/releases",
	}
	return json.dumps(data)

class AdminActivity(db.Model):
    id = db.Column(db.Integer, primary_key=True)
    time = db.Column(db.Integer, nullable=False, default=int(time.time()))
    message = db.Column(db.String(8192), nullable=True)

def DiscordWebhookMessage(message):

	newActivity = AdminActivity(
		time=int(time.time()),
		message=message
	)
	db.session.add(newActivity)
	db.session.commit()

	#check if the file exists
	if not os.path.exists("discordwebhookurl"):
		return
	else:
		with open("discordwebhookurl", "r") as f:
			url = f.read()
	try:
		webhook = discord_webhook.DiscordWebhook(url=url, content=message)
		webhook.execute()
	except:
		Log("admin", "Failed to send webhook message: " + message)
		pass

class Admin(UserMixin, db.Model):
    username: Mapped[str] = mapped_column(
        db.String(80),
        primary_key=True,
        unique=True,
        nullable=False
    )

    password: Mapped[str] = mapped_column(
        db.String(255),
        nullable=False
    )

    rank: Mapped[int] = mapped_column(
        db.Integer,
        nullable=False
    )

    email: Mapped[Optional[str]] = mapped_column(
        db.String(255),
        unique=True,
        nullable=True
    )

    email_verified: Mapped[bool] = mapped_column(
        db.Boolean,
        nullable=False,
        default=False
    )

    passkey_hash: Mapped[Optional[str]] = mapped_column(
        db.String(255),
        nullable=True
    )

    verification_code_hash: Mapped[Optional[str]] = mapped_column(
        db.String(255),
        nullable=True
    )

    verification_code_expires: Mapped[Optional[int]] = mapped_column(
        db.Integer,
        nullable=True
    )

    def get_id(self):
        return str(self.username)

def MigrateAdminSecurity():
    from sqlalchemy import inspect, text

    inspector = inspect(db.engine)

    if "admin" not in inspector.get_table_names():
        print("[Admin Migration] Admin table does not exist.")
        return

    existing_columns = {
        column["name"]
        for column in inspector.get_columns("admin")
    }

    migrations = {
        "email": """
            ALTER TABLE admin
            ADD COLUMN email VARCHAR(255)
        """,

        "email_verified": """
            ALTER TABLE admin
            ADD COLUMN email_verified BOOLEAN NOT NULL DEFAULT 0
        """,

        "passkey_hash": """
            ALTER TABLE admin
            ADD COLUMN passkey_hash VARCHAR(255)
        """,

        "verification_code_hash": """
            ALTER TABLE admin
            ADD COLUMN verification_code_hash VARCHAR(255)
        """,

        "verification_code_expires": """
            ALTER TABLE admin
            ADD COLUMN verification_code_expires INTEGER
        """
    }

    with db.engine.begin() as connection:

        for column_name, sql in migrations.items():

            if column_name in existing_columns:
                print(
                    f"[Admin Migration] "
                    f"'{column_name}' already exists. Skipping."
                )
                continue

            connection.execute(text(sql))

            print(
                f"[Admin Migration] "
                f"Added '{column_name}'."
            )

        connection.execute(text("""
            CREATE UNIQUE INDEX IF NOT EXISTS
            ix_admin_email_unique
            ON admin (email)
        """))

    print("[Admin Migration] Complete.")

@login_manager.user_loader
def load_user(user_id):
	return Admin.query.get(user_id)

@app.route("/admin", methods=['GET', 'POST'])
def AdminPage():

    # Create an admin account if one doesn't exist
    if not Admin.query.first():
        randompassword = ''.join(
            random.choices(
                string.ascii_letters + string.digits,
                k=24
            )
        )

        newAdmin = Admin(
            username="admin",
            password=bcrypt.generate_password_hash(
                randompassword
            ).decode('utf-8'),
            rank=0
        )

        db.session.add(newAdmin)
        db.session.commit()

        Log("server", "Created admin account")

        print(
            f"Admin account created! "
            f"Username: admin, Password: {randompassword}"
        )

    if request.method == 'GET':

        if current_user.is_authenticated:

            if not isAdmin(current_user):
                return abort(404)

            return redirect("/admin/home")

        else:
            return render_template('admin_login.html')


    if request.method == 'POST':

        # Get login method
        login_method = request.form.get(
            'login_method',
            'password'
        )


        # =========================================================
        # NORMAL PASSWORD LOGIN
        # =========================================================

        if login_method == 'password':

            username = request.form.get(
                'username',
                ''
            )

            password = request.form.get(
                'password',
                ''
            )

            username = re.sub(
                r'[^a-zA-Z0-9]',
                '',
                username
            )

            db_user = Admin.query.filter_by(
                username=username
            ).first()

            if db_user is None:
                return make_response(
                    "Invalid Username or Password!",
                    400
                )

            if not bcrypt.check_password_hash(
                db_user.password,
                password
            ):
                return make_response(
                    "Invalid Password or Username!",
                    400
                )


            # =========================================================
            # EMAIL LOGIN VERIFICATION
            # =========================================================

            # If email is verified, require 6-digit OTP
            if db_user.email_verified:

                # Make sure an email address exists
                if not db_user.email:

                    return make_response(
                        "Email verification is enabled, but no email address is configured for this account!",
                        400
                    )

                # Generate 6-digit OTP
                otp = f"{secrets.randbelow(1000000):06d}"

                # Hash OTP
                otp_hash = hashlib.sha256(
                    otp.encode('utf-8')
                ).hexdigest()

                # Store login OTP in session
                session['admin_login_otp_hash'] = otp_hash

                session['admin_login_user'] = db_user.username

                session['admin_login_otp_expires'] = (
                    datetime.utcnow() + timedelta(minutes=5)
                ).timestamp()

                try:

                    send_email_login_otp(
                        db_user.email,
                        otp
                    )

                except Exception as e:

                    # Clear login OTP session
                    session.pop(
                        'admin_login_otp_hash',
                        None
                    )

                    session.pop(
                        'admin_login_user',
                        None
                    )

                    session.pop(
                        'admin_login_otp_expires',
                        None
                    )

                    print(
                        "[LOGIN EMAIL ERROR]",
                        repr(e)
                    )

                    Log(
                        "admin",
                        "Failed to send admin login OTP: " + repr(e)
                    )

                    return make_response(
                        "Failed to send login verification email: " + str(e),
                        500
                    )

                Log(
                    "admin",
                    "Login verification code sent to admin email"
                )

                return redirect("/admin/verify-login")


            # =========================================================
            # EMAIL IS NOT VERIFIED
            # =========================================================

            login_user(
                db_user,
                remember=True
            )

            return redirect("/admin")


        # =========================================================
        # PASS-KEY LOGIN
        # =========================================================

        elif login_method == 'passkey':

            passkey = request.form.get(
                'passkey',
                ''
            ).strip()

            if not passkey:
                return make_response(
                    "Pass-Key is required!",
                    400
                )

            # The Pass-Key is only for the main admin account
            db_user = Admin.query.filter_by(
                username="admin"
            ).first()

            if db_user is None:
                return make_response(
                    "Admin account not found!",
                    404
                )

            # Make sure a Pass-Key has been generated
            if not db_user.passkey_hash:
                return make_response(
                    "Pass-Key has not been configured yet!",
                    400
                )

            # Check Pass-Key hash
            if not bcrypt.check_password_hash(
                db_user.passkey_hash,
                passkey
            ):
                return make_response(
                    "Invalid Pass-Key!",
                    400
                )

            # Pass-Key authentication successful
            login_user(
                db_user,
                remember=True
            )

            Log(
                "admin",
                "Admin logged in using Pass-Key"
            )

            return redirect("/admin")


        # =========================================================
        # INVALID LOGIN METHOD
        # =========================================================

        else:

            return make_response(
                "Invalid login method!",
                400
            )

def isAdmin(user):
	if not user.is_authenticated:
		return False
	db_user = Admin.query.filter_by(username=user.username).first()
	return db_user is not None

def send_email_change_otp(to_email, otp):
    msg = EmailMessage()

    msg["Subject"] = "Admin Email Change Verification Code"
    msg["From"] = SMTP_FROM
    msg["To"] = to_email

    msg.set_content(
        f"""Your email change verification code is:

{otp}

This code will expire in 5 minutes.

If you did not request this change, you can safely ignore this email.
"""
    )

    with smtplib.SMTP(SMTP_HOST, SMTP_PORT) as server:
        server.starttls()

        server.login(
            SMTP_USERNAME,
            SMTP_PASSWORD
        )

        server.send_message(msg)

def send_email_login_otp(to_email, otp):
    msg = EmailMessage()

    msg["Subject"] = "Admin Login Verification Code"
    msg["From"] = SMTP_FROM
    msg["To"] = to_email

    msg.set_content(
        f"""Your administrator login verification code is:

{otp}

This code will expire in 5 minutes.

If you did not attempt to login, please secure your account immediately.
"""
    )

    with smtplib.SMTP(
        SMTP_HOST,
        SMTP_PORT
    ) as server:

        server.starttls()

        server.login(
            SMTP_USERNAME,
            SMTP_PASSWORD
        )

        server.send_message(msg)

@app.route("/admin/verify-login", methods=['GET', 'POST'])
def AdminVerifyLogin():

    # =========================================================
    # GET
    # =========================================================

    if request.method == 'GET':

        if not session.get(
            'admin_login_otp_hash'
        ):

            return redirect("/admin")

        return render_template(
            'admin_login_otp.html'
        )


    # =========================================================
    # POST
    # =========================================================

    otp = request.form.get(
        'otp',
        ''
    ).strip()

    stored_hash = session.get(
        'admin_login_otp_hash'
    )

    username = session.get(
        'admin_login_user'
    )

    expires_at = session.get(
        'admin_login_otp_expires'
    )


    # =========================================================
    # CHECK OTP SESSION
    # =========================================================

    if not stored_hash or not username or not expires_at:

        return render_template(
            'admin_login_otp.html',
            error="No active login verification request found."
        )


    # =========================================================
    # CHECK EXPIRATION
    # =========================================================

    if datetime.utcnow().timestamp() > float(
        expires_at
    ):

        session.pop(
            'admin_login_otp_hash',
            None
        )

        session.pop(
            'admin_login_user',
            None
        )

        session.pop(
            'admin_login_otp_expires',
            None
        )

        return render_template(
            'admin_login_otp.html',
            error="The verification code has expired. Please login again."
        )


    # =========================================================
    # CHECK OTP FORMAT
    # =========================================================

    if len(otp) != 6 or not otp.isdigit():

        return render_template(
            'admin_login_otp.html',
            error="Verification code must be 6 digits."
        )


    # =========================================================
    # HASH ENTERED OTP
    # =========================================================

    entered_hash = hashlib.sha256(
        otp.encode('utf-8')
    ).hexdigest()


    # =========================================================
    # COMPARE OTP
    # =========================================================

    if not secrets.compare_digest(
        entered_hash,
        stored_hash
    ):

        return render_template(
            'admin_login_otp.html',
            error="Invalid verification code."
        )


    # =========================================================
    # OTP CORRECT
    # =========================================================

    db_user = Admin.query.filter_by(
        username=username
    ).first()


    if db_user is None:

        session.clear()

        return make_response(
            "Admin account not found!",
            404
        )


    # Login successful
    login_user(
        db_user,
        remember=True
    )


    # Clear OTP session
    session.pop(
        'admin_login_otp_hash',
        None
    )

    session.pop(
        'admin_login_user',
        None
    )

    session.pop(
        'admin_login_otp_expires',
        None
    )


    Log(
        "admin",
        "Admin logged in using password + email verification"
    )


    return redirect("/admin")

@login_required
@app.route("/admin/home")
def AdminHome():
	if not isAdmin(current_user):
		return abort(404)

	adminActivity = AdminActivity.query.order_by(AdminActivity.time).all()
	#convert time
	for log in adminActivity:
		log.time = datetime.fromtimestamp(log.time, tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S GMT')

	#reverse list
	adminActivity.reverse()

	return render_template('admin_home.html', Activity=adminActivity)

@login_required
@app.route("/admin/versions" , methods=['GET', 'POST'])
def AdminVersions():
	if not isAdmin(current_user):
		return abort(404)

	if request.method == 'GET':

		return render_template('admin_versions.html' , pc_version=open("data/persist/version.txt", "r").read(), android_version=open("data/persist/android_version.txt", "r").read())
	elif request.method == 'POST':
		form = request.form
		form = {k: v[0] if len(v) == 1 else v for k, v in form.items()}

		if "pc_version" not in form or form["pc_version"] == "":
			return make_response("Invalid PC version!", 400)
		if "android_version" not in form or form["android_version"] == "":
			return make_response("Invalid Android version!", 400)

		#update version.txt
		with open("data/persist/version.txt", "w") as f:
			f.write(form["pc_version"])
		with open("data/persist/android_version.txt", "w") as f:
			f.write(form["android_version"])

		return redirect("/admin/versions")

@login_required
@app.route("/admin/server")
def AdminServer():
	if not isAdmin(current_user):
		return abort(404)

	#create backup folder if it doesn't exist
	if not os.path.exists("backup"):
		os.makedirs("backup")

	#get last backup in folder
	last_backup_time = 0
	last_backup_file = ""
	for file in os.listdir("backup"):
		if file.endswith(".zip"):
			file_time = int(file.replace(".zip", "").replace("-", "").replace("_", ""))
			if file_time > last_backup_time:
				last_backup_time = file_time
				last_backup_file = file.replace(".zip", "")

	if last_backup_file == "":
		last_backup = "Never"
	else:
		last_backup = time_ago_string(datetime.strptime(last_backup_file, "%Y-%m-%d_%H-%M-%S"))

	return render_template('admin_server.html', last_backup=last_backup)

def time_ago_string(date_time):
    now = datetime.now()
    time_difference = now - date_time

    # Extracting hours and minutes
    hours = time_difference.seconds // 3600
    minutes = (time_difference.seconds // 60) % 60

    if time_difference.days > 0:
        return f"{time_difference.days} days ago"
    elif hours > 0:
        return f"{hours} {'hour' if hours == 1 else 'hours'} ago"
    elif minutes > 0:
        return f"{minutes} {'minute' if minutes == 1 else 'minutes'} ago"
    else:
        return f"{time_difference.seconds} seconds ago"

@login_required
@app.route("/admin/server/backup")
def AdminBackup():
	if not isAdmin(current_user):
		return abort(404)

	backup = Backup()
	if not backup:
		return make_response("Failed to backup", 400)
	return redirect("/admin/server")

@login_required
@app.route("/admin/server/pull")
def AdminGitPull():
	if not isAdmin(current_user):
		return abort(404)

	Log("admin", current_user.username + " pulled from git.")

	#Git pull and return response
	output = subprocess.check_output(["git", "pull"])

	Log("admin", "Pulled from git. Output: " + output.decode("utf-8"))

	#TODO: restart server

	return make_response(output.decode("utf-8"), 200)

@login_required
@app.route("/admin/createadmin", methods=['GET', 'POST'])
def AdminCreateAdmin():
	if not isAdmin(current_user):
		return abort(404)

	if request.method == 'GET':
		return render_template('admin_createadmin.html')

	if request.method == 'POST':
		username = request.form['username']
		rank = request.form['rank']

		#create random password
		password = secrets.token_urlsafe(24)

		new_admin = Admin(
			username=username,
			password=bcrypt.generate_password_hash(password).decode('utf-8'),
			rank=int(rank),

			# New security fields
			email=None,
			email_verified=False,
			passkey_hash=None,
			verification_code_hash=None,
			verification_code_expires=None
		)

		db.session.add(new_admin)
		db.session.commit()

		Log(
			"admin",
			current_user.username +
			" created admin: " +
			username +
			" with rank: " +
			rank
		)

		return "Admin created! Username: " + username + " Password: " + password

@app.route("/admin/secretprofile", methods=['GET', 'POST'])
@login_required
def AdminSecretProfile():

    # Make sure only admin users can access this page
    if not isAdmin(current_user):
        return abort(404)

    # Get the current admin account
    admin = Admin.query.filter_by(
        username=current_user.username
    ).first()

    if admin is None:
        return abort(404)

    # GET
    if request.method == 'GET':
        return render_template(
            'admin_secretprofile.html',
            admin=admin
        )

    action = request.form.get(
        'action',
        ''
    )

    # =========================================================
    # GENERATE PASS-KEY
    # =========================================================

    if action == 'generate_passkey':

        if admin.passkey_hash:

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="Pass-Key has already been generated and cannot be changed."
            )

        part1 = ''.join(
            random.choices(
                string.ascii_uppercase + string.digits,
                k=6
            )
        )

        part2 = ''.join(
            random.choices(
                string.ascii_uppercase + string.digits,
                k=6
            )
        )

        part3 = ''.join(
            random.choices(
                string.ascii_uppercase + string.digits,
                k=6
            )
        )

        passkey = (
            f"{part1}-"
            f"{part2}-"
            f"{part3}"
        )

        admin.passkey_hash = (
            bcrypt.generate_password_hash(
                passkey
            ).decode('utf-8')
        )

        db.session.commit()

        Log(
            "admin",
            "Admin Pass-Key generated"
        )

        return render_template(
            'admin_secretprofile.html',
            admin=admin,
            generated_passkey=passkey
        )


    # =========================================================
    # CHANGE PASSWORD
    # =========================================================

    elif action == 'change_password':

        current_password = request.form.get(
            'current_password',
            ''
        )

        new_password = request.form.get(
            'new_password',
            ''
        )

        confirm_password = request.form.get(
            'confirm_password',
            ''
        )

        if not bcrypt.check_password_hash(
            admin.password,
            current_password
        ):

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="Current password is incorrect."
            )

        if new_password != confirm_password:

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="New passwords do not match."
            )

        if len(new_password) < 12:

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="Password must be at least 12 characters long."
            )

        admin.password = (
            bcrypt.generate_password_hash(
                new_password
            ).decode('utf-8')
        )

        db.session.commit()

        Log(
            "admin",
            "Admin password changed"
        )

        return render_template(
            'admin_secretprofile.html',
            admin=admin,
            success="Password changed successfully."
        )


    # =========================================================
    # REQUEST EMAIL CHANGE OTP
    # =========================================================

    elif action == 'request_email_change':

        new_email = request.form.get(
            'new_email',
            ''
        ).strip().lower()

        if not new_email or '@' not in new_email:

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="Please enter a valid email address."
            )

        if admin.email and new_email == admin.email.lower():

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="This is already your current email address."
            )

        # Make sure SMTP configuration exists
        if not SMTP_HOST or not SMTP_USERNAME or not SMTP_PASSWORD:

            Log(
                "admin",
                "Email change OTP failed: SMTP configuration is missing."
            )

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error=(
                    "Email verification is currently unavailable. "
                    "The server email service is not configured."
                )
            )

        otp = f"{secrets.randbelow(1000000):06d}"

        otp_hash = hashlib.sha256(
            otp.encode('utf-8')
        ).hexdigest()

        session['email_change_otp_hash'] = otp_hash
        session['email_change_new_email'] = new_email
        session['email_change_otp_expires'] = (
            datetime.utcnow() + timedelta(minutes=5)
        ).timestamp()

        try:

            send_email_change_otp(
                new_email,
                otp
            )

        except smtplib.SMTPAuthenticationError as e:

            # SMTP authentication failed.
            # Usually caused by incorrect SMTP credentials
            # or missing/invalid Gmail App Password.

            session.pop(
                'email_change_otp_hash',
                None
            )

            session.pop(
                'email_change_new_email',
                None
            )

            session.pop(
                'email_change_otp_expires',
                None
            )

            print(
                "[EMAIL AUTH ERROR]",
                repr(e)
            )

            Log(
                "admin",
                "Email change OTP failed: SMTP authentication failed. "
                "Check SMTP username/password or App Password configuration. "
                + repr(e)
            )

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error=(
                    "Email verification is currently unavailable. "
                    "The server email authentication is not configured correctly."
                )
            )

        except smtplib.SMTPException as e:

            session.pop(
                'email_change_otp_hash',
                None
            )

            session.pop(
                'email_change_new_email',
                None
            )

            session.pop(
                'email_change_otp_expires',
                None
            )

            print(
                "[SMTP ERROR]",
                repr(e)
            )

            Log(
                "admin",
                "Email change OTP failed: SMTP error: "
                + repr(e)
            )

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error=(
                    "Failed to send verification email "
                    "due to a server email error."
                )
            )

        except Exception as e:

            session.pop(
                'email_change_otp_hash',
                None
            )

            session.pop(
                'email_change_new_email',
                None
            )

            session.pop(
                'email_change_otp_expires',
                None
            )

            print(
                "[EMAIL ERROR]",
                repr(e)
            )

            Log(
                "admin",
                "Failed to send email change OTP: "
                + repr(e)
            )

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="Failed to send verification email."
            )

        Log(
            "admin",
            "Email change OTP sent successfully to: "
            + new_email
        )

        return render_template(
            'admin_secretprofile.html',
            admin=admin,
            email_change_otp_sent=True,
            email_change_email=new_email,
            success=(
                "A 6-digit verification code has been sent "
                "to your new email address."
            )
        )


    # =========================================================
    # VERIFY EMAIL CHANGE OTP
    # =========================================================

    elif action == 'verify_email_change':

        otp = request.form.get(
            'otp',
            ''
        ).strip()

        stored_hash = session.get(
            'email_change_otp_hash'
        )

        new_email = session.get(
            'email_change_new_email'
        )

        expires_at = session.get(
            'email_change_otp_expires'
        )

        if not stored_hash or not new_email or not expires_at:

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error="No active email verification request found."
            )

        if datetime.utcnow().timestamp() > float(expires_at):

            session.pop(
                'email_change_otp_hash',
                None
            )

            session.pop(
                'email_change_new_email',
                None
            )

            session.pop(
                'email_change_otp_expires',
                None
            )

            Log(
                "admin",
                "Email change OTP expired."
            )

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                error=(
                    "The verification code has expired. "
                    "Please request a new code."
                )
            )

        if len(otp) != 6 or not otp.isdigit():

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                email_change_otp_sent=True,
                email_change_email=new_email,
                error="Verification code must be 6 digits."
            )

        entered_hash = hashlib.sha256(
            otp.encode('utf-8')
        ).hexdigest()

        if not secrets.compare_digest(
            entered_hash,
            stored_hash
        ):

            Log(
                "admin",
                "Invalid email change OTP entered."
            )

            return render_template(
                'admin_secretprofile.html',
                admin=admin,
                email_change_otp_sent=True,
                email_change_email=new_email,
                error="Invalid verification code."
            )

        # =====================================================
        # OTP VALID
        # =====================================================

        # Update email
        admin.email = new_email

        # Email is verified because the user
        # successfully received and entered the OTP.
        admin.email_verified = True

        db.session.commit()

        # Clear OTP session
        session.pop(
            'email_change_otp_hash',
            None
        )

        session.pop(
            'email_change_new_email',
            None
        )

        session.pop(
            'email_change_otp_expires',
            None
        )

        Log(
            "admin",
            "Admin email changed and verified successfully."
        )

        return render_template(
            'admin_secretprofile.html',
            admin=admin,
            success="Email address changed successfully."
        )


    # =========================================================
    # INVALID ACTION
    # =========================================================

    return render_template(
        'admin_secretprofile.html',
        admin=admin,
        error="Invalid action."
    )

@app.route("/admin/players")
@login_required
def AdminPlayers():
	if not isAdmin(current_user):
		return abort(404)

	players = Player.query.all()

	#convert player to dict
	players = [player.as_dict() for player in players]

	#remove any players that do not have a multiplayer name
	players = [player for player in players if player["game"] != None and player["leader_level"] != None]

	#remove any player that is banned
	players = [player for player in players if not IsUserBanned(player["username"])]

	for player in players:
		player["last_online"] = datetime.fromtimestamp(player["last_online"], tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S UTC')

		#if player's multiplayer name is empty, attempt to get it from their game
		if player["multiplayer_name"] == None:
			player["multiplayer_name"] = GetNameFromSave(player["game"])

	sortQuery = request.args.get('sort')

	if sortQuery is not None:
		players = sorted(players, key=lambda player: player[sortQuery], reverse=True)
	else:
		players = players[::-1]

	return render_template('admin_players.html', players=players, player_count=len(players))

def GetNameFromSave(save):

	try:
		game = DecryptGameData(save)
	except Exception:
		return None
	if game is None:
		return None
	return game.get("MultiplayerPlayerName")

@app.route("/online_players")
def AdminOnlinePlayers():


    players = Player.query.all()
    players = [player.as_dict() for player in players]

    # Get the current time minus five minutes (in UTC)
    current_time = datetime.now(timezone.utc)
    five_minutes_ago = current_time - timedelta(minutes=5)

    online_players = []

    # Log and check player last_online times
    for player in players:
        # Convert any bytes fields to string (e.g., multiplayer_name, game, etc.)
        for key, value in player.items():
            if isinstance(value, bytes):
                player[key] = value.decode('utf-8')  # Decode bytes to string

        if player["last_online"]:
            last_online = datetime.fromtimestamp(player["last_online"], tz=timezone.utc)
            if last_online >= five_minutes_ago:
                online_players.append(player)

    return {
        "online_player_count": len(online_players)
    }


@app.route("/admin/players/<player>")
@login_required
def AdminPlayer(player):
	if not isAdmin(current_user):
		return abort(404)

	player = Player.query.filter_by(username=player).first()

	if player is None:
		return make_response("No player found!", 404)

	player = player.as_dict()

	player["last_online"] = datetime.fromtimestamp(player["last_online"], tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S GMT')

	game = None
	try:
		game = DecryptGameData(player["game"])
		if player["multiplayer_name"] == None:
			player["multiplayer_name"] = game["MultiplayerPlayerName"]
	except Exception:
		Log("admin", "Failed to decrypt player game data for player: " + player["username"])
		game = None

	if game is None:
		return render_template('admin_player.html', player=player)

	battle_history = game["BattleHistory"]
	battle_history.sort(key=lambda x: x["recordTime"])

	for battle in battle_history:
		battle["recordTime"] = datetime.fromtimestamp(battle["recordTime"], tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S GMT')

	#fix device name
	if player["devicename"] is not None:
		player["devicename"] = re.sub(r'%[0-9A-Fa-f]{2}', lambda m: chr(int(m.group(0)[1:], 16)), player["devicename"])

	Inventory = game["Inventory"]

	#remove all items that are not creatures
	if Inventory is not None:
		Inventory = [item for item in Inventory if item["_T"] == "CR"]

	return render_template('admin_player.html', player=player, is_banned=IsUserBanned(player["username"]), SoftCurrency=game["SoftCurrency"], HardCurrency=int(game["PaidHardCurrency"]) + int(game["FreeHardCurrency"]), PvpCurrency=game["PvpCurrency"], InstalledDate=datetime.fromtimestamp(game["InstalledDate"], tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S GMT'), PVPBanned=bool(game["Zxcvbnm"]), MultiplayerLevel=game["MultiplayerLevel"], InventorySpace=game["InventorySpace"], BattleHistory=battle_history, DeviceName=player["devicename"], Inventory=Inventory)

@app.route("/admin/players/<player>/game")
@login_required
def AdminPlayerGame(player):

    print("========================================")
    print("[AdminPlayerGame] ROUTE HIT")
    print("[AdminPlayerGame] player:", repr(player))
    print("[AdminPlayerGame] current_user:", current_user.username)
    print("========================================")

    if not isAdmin(current_user):
        print("[AdminPlayerGame] NOT ADMIN")
        return abort(404)

    player_obj = Player.query.filter_by(username=player).first()

    print(
        "[AdminPlayerGame] player found:",
        player_obj is not None
    )

    if player_obj is None:
        print(
            "[AdminPlayerGame] PLAYER NOT FOUND:",
            repr(player)
        )
        return make_response("No player found!", 404)

    player = player_obj.as_dict()

    try:
        game = DecryptGameData(player["game"])
    except Exception:
        # save is most likely not encrypted
        game = player["game"]

    if game is None:
        return make_response("No game found!", 404)

    # Convert game object to JSON
    game_json = json.dumps(
        game,
        ensure_ascii=False,
        default=lambda x:
            x.decode("utf-8", errors="ignore")
            if isinstance(x, bytes)
            else str(x)
    )

    # Remove unnecessary escaping if present
    if game_json.startswith('"') and game_json.endswith('"'):
        game_json = game_json[1:-1]

    game_json = game_json.replace('\\"', '"')

    return render_template(
        'admin_player_game.html',
        game=game_json,
        player_id=player["username"]
    )

@app.route("/admin/players/<player>/game/edit", methods=['POST'])
@login_required
def AdminPlayerGameEdit(player):
    if not isAdmin(current_user):
        return abort(404)

    player = Player.query.filter_by(username=player).first()

    if player is None:
        return make_response("No player found!", 404)

    # Get game data from POST request
    game = request.form.get('player_game')
    if game is None:
        return make_response("No game data provided!", 400)

    print("Received game data:", game)

    try:
        # Update game data
        player.game = game
        db.session.commit()
        print("Game data updated successfully.")
    except Exception as e:
        db.session.rollback()  # Rollback if something goes wrong
        print("Error updating game data:", str(e))
        return make_response("Failed to update game data.", 500)

    Log("admin", current_user.username + " edited game data for player: " + player.username)
    return redirect("/admin/players/" + player.username)


def DecryptGameData(game:str):

	if game is None or game == b"" or game == b" ":

		return None

	# Decrypt

	input_data = game

	# if input_data is bytes, decode safely later

	try:

		input_str = input_data.decode('utf-8') if isinstance(input_data, (bytes, bytearray)) else str(input_data)

		index = input_str.find("&data=")

		if index == -1:

			return None

		encoded_data = input_str[index + 6:]

		array = base64.b64decode(encoded_data)

		with gzip.GzipFile(fileobj=BytesIO(array), mode='rb') as gz:

			decoded_data = gz.read().decode('utf-8')

	except Exception:

		Log("admin", "Failed to decrypt game data")

		return None

	# attempt to clean json

	decoded_data = decoded_data.replace(',}', '}').replace(',],', '],').replace(',]', ']').replace(',,', ',')

	return json.loads(decoded_data)

@app.route("/admin/pvp_leaderboard")
@login_required
def AdminPvpLeaderboard():
    if not isAdmin(current_user):
        return abort(404)

    leaderboard = []

    for player in Player.query.all():
        try:
            game = DecryptGameData(player.game)

            if game is None:
                continue

            leaderboard.append({
                "username": player.username,
                "player_name": game.get("MultiplayerPlayerName", "Unknown"),
                "multiplayer_level": int(game.get("MultiplayerLevel", 999)),
                "points": int(game.get("PointsInMultiplayerLevel", 0)),
            })

        except Exception:
            continue

    # Önce MultiplayerLevel, sonra aynı level içinde puan
    leaderboard.sort(
        key=lambda x: (
            x["multiplayer_level"] == -1,  # False önce, True sonra
            x["multiplayer_level"],        # 1,2,3,...,26
            -x["points"]                   # Aynı level içinde yüksek puan önce
        )
    )

    return render_template(
        "admin_pvp_leaderboard.html",
        leaderboard=leaderboard
    )

# -----------------------------
# API endpoint for granting rewards (used by Discord bot)
# -----------------------------
# POST /api/grant_reward
# Body (JSON):
#   - cardwars_id: string (required)
#   - reward_type: string (required, currently only "Gems" supported)
#   - amount: integer (required)
#   - admin_user: string (optional if using API key)
#   - admin_pass: string (optional if using API key)
#
# Or send header: X-API-KEY: <PYAN_API_KEY>
#
# IMPORTANT: This file does not contain any hardcoded credentials. Set the following
# environment variables on PythonAnywhere (recommended):
#   - PYAN_API_KEY          (preferred)  -> bot should send this in X-API-KEY header
#   - PYAN_ADMIN_USER       (optional)  -> fallback to admin_user/admin_pass in JSON
#   - PYAN_ADMIN_PASS       (optional)
#
# On the bot (Railway) put bot credentials there (DB_LOGIN, DB_PASSWORD) and the bot
# will send them in the request body (or use an API key approach).
@app.route("/api/grant_reward", methods=['POST'])
def ApiGrantReward():
    try:
        payload = request.get_json(silent=True)
        if not payload:
            return jsonify({'success': False, 'error': 'JSON body required'}), 400

        # AUTH: prefer API key header, fallback to admin_user/admin_pass
        api_key_header = request.headers.get("X-API-KEY")
        configured_api_key = os.environ.get("PYAN_API_KEY")
        authorized = False

        if configured_api_key and api_key_header and api_key_header == configured_api_key:
            authorized = True
        else:
            # fallback to user/pass check
            admin_user = payload.get("admin_user")
            admin_pass = payload.get("admin_pass")
            expected_user = os.environ.get("PYAN_ADMIN_USER")
            expected_pass = os.environ.get("PYAN_ADMIN_PASS")
            if expected_user and expected_pass:
                if admin_user == expected_user and admin_pass == expected_pass:
                    authorized = True
                else:
                    authorized = False
            else:
                # If no expected creds are configured on server AND no API key,
                # deny to avoid accidental open endpoint.
                return jsonify({'success': False, 'error': 'Server auth not configured (set PYAN_API_KEY or PYAN_ADMIN_USER/PYAN_ADMIN_PASS)'}), 500

        if not authorized:
            return jsonify({'success': False, 'error': 'Unauthorized'}), 401

        # validate payload
        cardwars_id = payload.get("cardwars_id")
        reward_type = payload.get("reward_type")
        amount = payload.get("amount")

        if not cardwars_id or not reward_type or amount is None:
            return jsonify({'success': False, 'error': 'cardwars_id, reward_type and amount are required'}), 400
        try:
            amount = int(amount)
        except Exception:
            return jsonify({'success': False, 'error': 'amount must be an integer'}), 400
        if amount <= 0:
            return jsonify({'success': False, 'error': 'amount must be > 0'}), 400

        # currently only Gems supported (extend as needed)
        if reward_type != "Gems":
            return jsonify({'success': False, 'error': f'Unsupported reward_type: {reward_type}'}), 400

        # find player by multiplayer_name OR username, try case-insensitive match
        player = Player.query.filter_by(multiplayer_name=cardwars_id).first()
        if player is None:
            player = Player.query.filter_by(username=cardwars_id).first()
        if player is None:
            # try lower-case match on multiplayer_name
            player = Player.query.filter(func.lower(Player.multiplayer_name) == cardwars_id.lower()).first()

        if player is None:
            return jsonify({'success': False, 'error': 'Player not found'}), 404

        # get current save (player.game)
        game_field = player.game
        if game_field is None:
            save_json = {}
        else:
            # decode bytes if needed
            game_text = None
            if isinstance(game_field, (bytes, bytearray)):
                try:
                    game_text = game_field.decode('utf-8')
                except Exception:
                    game_text = None
            else:
                game_text = game_field

            save_json = None
            # try parse as JSON string
            if isinstance(game_text, str) and game_text.strip().startswith('{'):
                try:
                    save_json = json.loads(game_text)
                except Exception:
                    save_json = None

            if save_json is None:
                # try decrypting using existing DecryptGameData
                try:
                    decrypted = DecryptGameData(game_field)
                    if isinstance(decrypted, dict):
                        save_json = decrypted
                    else:
                        if isinstance(decrypted, str) and decrypted.strip().startswith('{'):
                            save_json = json.loads(decrypted)
                except Exception:
                    save_json = None

            if save_json is None:
                # as last resort, initialize empty save
                save_json = {}

        # ensure FreeHardCurrency exists and is integer
        try:
            prev_amount = int(save_json.get("FreeHardCurrency", 0) or 0)
        except Exception:
            prev_amount = 0
        new_amount = prev_amount + amount
        save_json["FreeHardCurrency"] = new_amount

        # write back save as JSON string (AdminPlayerGameEdit-like behavior)
        try:
            player.game = json.dumps(save_json, ensure_ascii=False)
            db.session.commit()
        except Exception as e:
            db.session.rollback()
            return jsonify({'success': False, 'error': f'Failed to write save: {str(e)}'}), 500

        # Optional: log admin activity
        Log("admin", f"API grant: +{amount} FreeHardCurrency to {player.username} (by API)")

        return jsonify({
            'success': True,
            'player': player.username,
            'previous': prev_amount,
            'new': new_amount
        }), 200

    except Exception as e:
        return jsonify({'success': False, 'error': f'Internal error: {str(e)}'}), 500

        # -----------------------------
# API endpoint for adding a creature (used by Discord bot)
# -----------------------------
# POST /api/add_creature
# Body (JSON):
#   - cardwars_id: string (required)
#   - creature_id: string (required)
#   - star_rating: integer (optional, 1..5)
#   - admin_user: string (optional if using API key)
#   - admin_pass: string (optional if using API key)
#
# Or header: X-API-KEY: <PYAN_API_KEY>
@app.route("/api/add_creature", methods=['POST'])
def ApiAddCreature():
    try:
        payload = request.get_json(silent=True)
        if not payload:
            return jsonify({'success': False, 'error': 'JSON body required'}), 400

        # AUTH: prefer API key header, fallback to admin_user/admin_pass
        api_key_header = request.headers.get("X-API-KEY")
        configured_api_key = os.environ.get("PYAN_API_KEY")
        authorized = False

        if configured_api_key and api_key_header and api_key_header == configured_api_key:
            authorized = True
        else:
            admin_user = payload.get("admin_user")
            admin_pass = payload.get("admin_pass")
            expected_user = os.environ.get("PYAN_ADMIN_USER")
            expected_pass = os.environ.get("PYAN_ADMIN_PASS")
            if expected_user and expected_pass:
                if admin_user == expected_user and admin_pass == expected_pass:
                    authorized = True
                else:
                    authorized = False
            else:
                # If no expected creds are configured on server AND no API key,
                # deny to avoid accidental open endpoint.
                return jsonify({'success': False, 'error': 'Server auth not configured (set PYAN_API_KEY or PYAN_ADMIN_USER/PYAN_ADMIN_PASS)'}), 500

        if not authorized:
            return jsonify({'success': False, 'error': 'Unauthorized'}), 401

        cardwars_id = payload.get("cardwars_id")
        creature_id = payload.get("creature_id")
        star_rating = payload.get("star_rating", None)

        if not cardwars_id or not creature_id:
            return jsonify({'success': False, 'error': 'cardwars_id and creature_id are required'}), 400

        # validate star_rating if provided
        if star_rating is not None:
            try:
                star_rating = int(star_rating)
            except Exception:
                return jsonify({'success': False, 'error': 'star_rating must be an integer'}), 400
            if star_rating < 1 or star_rating > 5:
                return jsonify({'success': False, 'error': 'star_rating must be between 1 and 5'}), 400
        else:
            star_rating = 1

        # find player by multiplayer_name OR username (case-insensitive similar to grant)
        player = Player.query.filter_by(multiplayer_name=cardwars_id).first()
        if player is None:
            player = Player.query.filter_by(username=cardwars_id).first()
        if player is None:
            player = Player.query.filter(func.lower(Player.multiplayer_name) == cardwars_id.lower()).first()

        if player is None:
            return jsonify({'success': False, 'error': 'Player not found'}), 404

        # load player's save (player.game) with same method as grant endpoint
        game_field = player.game
        save_json = None

        if game_field is None:
            save_json = {}
        else:
            game_text = None
            if isinstance(game_field, (bytes, bytearray)):
                try:
                    game_text = game_field.decode('utf-8')
                except Exception:
                    game_text = None
            else:
                game_text = game_field

            # try parse JSON
            if isinstance(game_text, str) and game_text.strip().startswith('{'):
                try:
                    save_json = json.loads(game_text)
                except Exception:
                    save_json = None

            if save_json is None:
                try:
                    decrypted = DecryptGameData(game_field)
                    if isinstance(decrypted, dict):
                        save_json = decrypted
                    else:
                        if isinstance(decrypted, str) and decrypted.strip().startswith('{'):
                            save_json = json.loads(decrypted)
                except Exception:
                    save_json = None

        if save_json is None:
            save_json = {}

        # ensure Inventory array exists
        inventory = save_json.get("Inventory")
        if inventory is None or not isinstance(inventory, list):
            inventory = []
            save_json["Inventory"] = inventory

        # compute new UniqueID: max present UniqueID + 1 (or 1 if none)
        max_uid = 0
        for it in inventory:
            try:
                uid = int(it.get("UniqueID", 0))
                if uid > max_uid:
                    max_uid = uid
            except Exception:
                continue
        new_uid = max_uid + 1 if max_uid >= 0 else 1

        # build creature object
        creature_obj = {
            "_T": "CR",
            "ID": str(creature_id),
            "UniqueID": int(new_uid),
            "Xp": 0,
            "Favorite": 0,
            "Passive": 1,
            "PassiveFeeds": 0,
            "StarRating": int(star_rating)
        }

        # append and save
        inventory.append(creature_obj)

        # write back save as JSON string
        try:
            player.game = json.dumps(save_json, ensure_ascii=False)
            db.session.commit()
        except Exception as e:
            db.session.rollback()
            return jsonify({'success': False, 'error': f'Failed to write save: {str(e)}'}), 500

        # log admin activity
        Log("admin", f"API add creature: {creature_id} (Star {star_rating}) UniqueID={new_uid} to {player.username}")

        return jsonify({
            'success': True,
            'player': player.username,
            'unique_id': new_uid,
            'starRating': star_rating,
            'previous_inventory_count': max_uid and (len(inventory)-1) or 0,
            'new_inventory_count': len(inventory)
        }), 200

    except Exception as e:
        return jsonify({'success': False, 'error': f'Internal error: {str(e)}'}), 500


# Supported temporary ban durations in seconds.
BAN_DURATIONS = {
	"30m": 30 * 60,
	"1h": 1 * 60 * 60,
	"2h": 2 * 60 * 60,
	"4h": 4 * 60 * 60,
	"6h": 6 * 60 * 60,
	"8h": 8 * 60 * 60,
	"12h": 12 * 60 * 60,
	"16h": 16 * 60 * 60,
	"24h": 24 * 60 * 60,
	"2d": 2 * 24 * 60 * 60,
	"3d": 3 * 24 * 60 * 60,
	"4d": 4 * 24 * 60 * 60,
	"7d": 7 * 24 * 60 * 60,
	"2w": 14 * 24 * 60 * 60,
	"1mo": 30 * 24 * 60 * 60,
}


def CreateOrUpdatePlayerBan(player, duration_key=None):
	"""Create/update a player ban. None means permanent."""
	now = int(time.time())
	ban = Bans.query.filter_by(username=player).first()

	if ban is None:
		ban = Bans(
			username=player,
			bantype="userid",
			author=current_user.username,
			time=now
		)
		db.session.add(ban)

	ban.bantype = "userid"
	ban.author = current_user.username
	ban.time = now

	if duration_key is None or duration_key == "permanent":
		ban.expires_at = None
		return "permanent"

	if duration_key not in BAN_DURATIONS:
		raise ValueError("Invalid ban duration")

	ban.expires_at = now + BAN_DURATIONS[duration_key]
	return duration_key


@login_required
@app.route("/admin/players/<player>/<action>")
@app.route("/admin/players/<player>/<action>/<duration>")
def AdminPlayerAction(player, action, duration=None):
	if not isAdmin(current_user):
		return abort(404)

	if action == "ban":
		# Old /ban URL remains permanent for compatibility.
		try:
			ban_type = CreateOrUpdatePlayerBan(player, duration)
		except ValueError:
			return make_response("Invalid ban duration!", 400)

		db.session.commit()

		message = (
			current_user.username + " banned ID: " + player +
			(" permanently" if ban_type == "permanent" else " for " + ban_type)
		)

	elif action == "unban":
		player_check = Bans.query.filter_by(username=player).first()

		if player_check is not None:
			db.session.delete(player_check)
			db.session.commit()

		message = current_user.username + " unbanned ID: " + player

	else:
		return make_response("Invalid action!", 400)

	Log("admin", message)
	DiscordWebhookMessage(message)
	return redirect("/admin/players/" + player)

def SystemBan(username):
	Log("admin", "SYSTEM BANNED " + username)
	if not IsUserBanned(username):
		newban = Bans(username=username, bantype="userid", author="SYSTEM", time=int(time.time()))
		db.session.add(newban)
		db.session.commit()
		DiscordWebhookMessage("SYSTEM performed ban on ID: " + username)


@app.route("/admin/players/transfer", methods=['POST'])
def AdminPlayerTransfer():
    # Fetch API key from the request headers
    api_key = request.headers.get("API-Key")

    # Validate the API key
    if api_key != "9df81b2c-4f3a-41c7-8a3e-e28c0f6d9c49":
        return jsonify({"error": "Unauthorized access"}), 403

    # Get the player IDs and leader_level from the request
    data = request.json
    id1 = data.get("id1")
    id2 = data.get("id2")
    leader_level = data.get("leader_level")

    if not id1 or not id2 or not leader_level:
        return jsonify({"error": "Both player IDs and leader level are required"}), 400

    # Check if the IDs are the same
    if id1 == id2:
        return jsonify({"error": "Player IDs cannot be the same for both players"}), 400

    # Fetch players
    player1 = Player.query.filter_by(username=id1).first()
    player2 = Player.query.filter_by(username=id2).first()

    if not player1 or not player2:
        return jsonify({"error": "One or both players not found"}), 404

    try:
        # Convert the leader_level to an integer (if it isn't already)
        leader_level = int(leader_level.strip())  # Strip any extra spaces and convert to integer

        # Check if the leader level matches
        if player1.leader_level != leader_level:
            return jsonify({"error": f"Rank does not match for player {id1}. Make sure you enter the correct one."}), 400

        # Directly transfer the game data (without decryption)
        if player1.game:
            player2.game = player1.game  # Transfer the .bin file as is, no decryption needed
        else:
            logging.error(f"No game data found for {id1}")
            return jsonify({"error": "No game data to transfer from player 1"}), 400

        db.session.commit()

        logging.info(f"Transferred progress from {id1} to {id2}")
        return jsonify({"success": f"Progress transferred from {id1} to {id2}"}), 200

    except Exception as e:
        logging.error(f"Error during transfer: {str(e)}")
        return jsonify({"error": f"An error occurred: {str(e)}"}), 500

@login_required
@app.route("/admin/ipban/<ip>/unban")
def AdminIPBan(ip):
	if not isAdmin(current_user):
		return abort(404)

	Log("admin", current_user.username + " performed unban on " + ip)

	player_check = Bans.query.filter_by(username=ip).first()
	db.session.delete(player_check)
	db.session.commit()

	DiscordWebhookMessage(current_user.username + " performed unban on IP: " + ip)
	return redirect("/admin/bannedips")

@login_required
@app.route("/admin/ipban", methods=['POST'])
def AdminIPBanAction():
	if not isAdmin(current_user):
		return abort(404)

	Log("admin", current_user.username + " performed ban on " + request.form['ip'])

	newban = Bans(username=request.form['ip'], bantype="ip", author=current_user.username, time=int(time.time()))
	db.session.add(newban)
	db.session.commit()

	DiscordWebhookMessage(current_user.username + " performed ban on IP: " + request.form['ip'])
	return redirect("/admin/bannedips")


@login_required
@app.route("/admin/bannedplayers")
def AdminBannedPlayers():
	if not isAdmin(current_user):
		return abort(404)

	# Remove expired temporary bans before displaying the list.
	active_bans = []
	for ban in Bans.query.filter_by(bantype="userid").all():
		if IsBanActive(ban):
			active_bans.append(ban)
		else:
			db.session.delete(ban)

	db.session.commit()

	bans = [ban.as_dict() for ban in active_bans]

	#get player name
	for ban in bans:
		player_obj = Player.query.filter_by(username=ban["username"]).first()

		if player_obj is not None:
			ban["multiplayer_name"] = player_obj.multiplayer_name
			if ban["multiplayer_name"] is None:
				ban["multiplayer_name"] = GetNameFromSave(player_obj.game)
		else:
			ban["multiplayer_name"] = None

		ban["duration"] = "Permanent"

		if ban["expires_at"] is not None:
			ban["duration"] = datetime.fromtimestamp(
				ban["expires_at"],
				tz=timezone.utc
			).strftime('%Y-%m-%d %H:%M:%S GMT')

		if ban["time"] is not None:
			ban["time"] = datetime.fromtimestamp(
				ban["time"],
				tz=timezone.utc
			).strftime('%Y-%m-%d %H:%M:%S GMT')

	return render_template('admin_bannedplayers.html', bans=bans)

@login_required
@app.route("/admin/bannedips")
def AdminBannedIPs():
	if not isAdmin(current_user):
		return abort(404)

	bans = Bans.query.filter_by(bantype="ip").all()
	bans = [ban.as_dict() for ban in bans]

	for ban in bans:
		ban["time"] = datetime.fromtimestamp(ban["time"], tz=timezone.utc).strftime('%Y-%m-%d %H:%M:%S GMT')
	return render_template('admin_bannedips.html', bans=bans)

@login_required
@app.route("/admin/maintenance")
def AdminMaintenance():
	if not isAdmin(current_user):
		return abort(404)

	return render_template('admin_maintenance.html', maintenance=maintenance)

@login_required
@app.route("/admin/maintenance/<action>")
def AdminMaintenanceAction(action):
	if not isAdmin(current_user):
		return abort(404)

	global maintenance
	if action == "enable":
		maintenance = True
	elif action == "disable":
		maintenance = False
	Log("admin", current_user.username + " updated maintenance mode to " + ("on" if maintenance else "off"))
	return redirect("/admin/maintenance")

@app.route("/admin/logout")
def AdminLogout():
	logout_user()
	return redirect("/admin")

def Backup():
	#get date and time
	now = datetime.now().strftime("%Y-%m-%d_%H-%M-%S")
	os.makedirs("backup/" + now, exist_ok=True)

	# copy database safely
	db_path = "instance/cardwarskingdom.db"
	if os.path.exists(db_path):
		shutil.copy(db_path, f"backup/{now}/cardwarskingdom.db")
	else:
		Log("admin", "Database file missing during backup!")
		return False

	#copy persist folder
	shutil.copytree("data/persist", f"backup/{now}/persist")

	#zip
	shutil.make_archive("backup/" + now, 'zip', "backup/" + now)

	#delete folder
	shutil.rmtree("backup/" + now)

	Log("admin", "Backed up")
	return True

@login_required
@app.route("/admin/misc")
def AdminMisc():
	if not isAdmin(current_user):
		return abort(404)

	return render_template('admin_misc.html')

@login_required
@app.route("/admin/logs/delete/olderthan/<days>")
def AdminLogsDeleteOlderThan(days):
	if not isAdmin(current_user):
		return abort(404)

	#convert days to seconds
	days = int(days)
	seconds = days * 86400

	#delete logs older than x days
	db.session.query(Logs).filter(Logs.time < int(time.time()) - seconds).delete()
	db.session.commit()

	Log("admin", current_user.username + " deleted logs older than " + str(days))

	return redirect("/admin/logs")

@login_required
@app.route("/admin/upsight/delete/olderthan/<days>")
def AdminUpsightDeleteOlderThan(days):
	if not isAdmin(current_user):
		return abort(404)

	#convert days to seconds
	days = int(days)
	seconds = days * 86400

	#delete logs older than x days
	db.session.query(UpsightLogs).filter(UpsightLogs.time < int(time.time()) - seconds).delete()
	db.session.commit()

	Log("admin", current_user.username + " deleted upsight logs older than " + str(days))

	return redirect("/admin/upsight")

@login_required
@app.route("/admin/logs", methods=['GET'])
def AdminLogs():
	if not isAdmin(current_user):
		return abort(404)

	perpage = 20
	pagerequest = request.args.get('page', 1, type=int)
	query = request.args.get('query', '', type=str)
	logs = db.paginate(db.select(Logs).order_by(Logs.id.desc()), page=pagerequest, per_page=perpage)

	if query != '':
		logs = db.paginate(db.select(Logs).filter(Logs.player == query).order_by(Logs.id.desc()), page=pagerequest, per_page=perpage)

	return render_template('admin_logs.html', logs=logs, query=query)

@login_required
@app.route("/admin/upsight", methods=['GET'])
def AdminUpsight():
	if not isAdmin(current_user):
		return abort(404)

	perpage = 20
	pagerequest = request.args.get('page', 1, type=int)
	query = request.args.get('query', '', type=str)
	logs = db.paginate(db.select(UpsightLogs).order_by(UpsightLogs.id.desc()), page=pagerequest, per_page=perpage)

	if query != '':
		logs = db.paginate(db.select(UpsightLogs).filter(UpsightLogs.player_id == query).order_by(UpsightLogs.id.desc()), page=pagerequest, per_page=perpage)

	#convert time
	for log in logs.items:
		log.time = datetime.fromtimestamp(log.time)

	return render_template('admin_upsight.html', logs=logs, query=query)

class Bans(db.Model):
	username = db.Column(db.String(80), primary_key=True)
	bantype = db.Column(db.String(80), nullable=False)
	author = db.Column(db.String(80), nullable=True)
	time = db.Column(db.Integer, nullable=True, default=int(time.time()))

	# None = permanent ban.
	# Unix timestamp = temporary ban expiration time.
	expires_at = db.Column(db.Integer, nullable=True, default=None)

	def as_dict(self):
		return {c.name: getattr(self, c.name) for c in self.__table__.columns}


def MigrateBanExpiration():
	"""Add expires_at to existing SQLite ban tables without deleting old bans."""
	from sqlalchemy import inspect, text

	inspector = inspect(db.engine)

	if "bans" not in inspector.get_table_names():
		return

	existing_columns = {
		column["name"]
		for column in inspector.get_columns("bans")
	}

	if "expires_at" not in existing_columns:
		with db.engine.begin() as connection:
			connection.execute(text(
				"ALTER TABLE bans ADD COLUMN expires_at INTEGER"
			))
		print("[Ban Migration] Added 'expires_at' column.")

class Logs(db.Model):
	id = db.Column(db.Integer, primary_key=True)
	date = db.Column(db.String(80), nullable=False)
	time = db.Column(db.String(80), nullable=False)
	player = db.Column(db.String(80), nullable=False)
	ip = db.Column(db.String(80), nullable=True)
	message = db.Column(db.String(8192), nullable=False)

class UpsightLogs(db.Model):
	id = db.Column(db.Integer, primary_key=True)
	player_id = db.Column(db.String(80), nullable=False)
	time = db.Column(db.Integer, nullable=False, default=int(time.time()))
	event = db.Column(db.String(80), nullable=False)
	action = db.Column(db.String(80), nullable=False)
	message = db.Column(db.String(1024), nullable=True)

def PlayerLog(ip:str,player:str, message:str):
	db_log = Logs(date=datetime.now().strftime("%Y-%m-%d"), time=datetime.now().strftime("%H:%M:%S"), player=player, ip=ip, message=message)
	db.session.add(db_log)
	db.session.commit()

def IPFromRequest(request:Request):
	ip = request.remote_addr
	if request.headers.getlist("X-Forwarded-For"):
		ip = request.headers.getlist("X-Forwarded-For")[0]
	return ip

class Player(db.Model):
	username = db.Column(db.String(80), primary_key=True, unique=True, nullable=False)
	game = db.Column(db.String(8192), nullable=True)
	multiplayer_name = db.Column(db.String(128), nullable=True)
	icon = db.Column(db.String(128), nullable=True)
	deck = db.Column(db.String(1024), nullable=True)
	deck_rank = db.Column(db.String(16), nullable=True)
	landscapes = db.Column(db.String(1024), nullable=True)
	helper_creature = db.Column(db.String(1024), nullable=True)
	leader = db.Column(db.String(128), nullable=True)
	leader_level = db.Column(db.Integer, nullable=True)
	allyboxspace = db.Column(db.Integer, nullable=True)
	level = db.Column(db.Integer, nullable=True)
	friends = db.Column(db.String(8192), nullable=True, default="[]")
	friend_requests = db.Column(db.String(8192), nullable=True, default="[]")
	last_online = db.Column(db.Integer, nullable=True, default=int(time.time()))
	helpcount = db.Column(db.Integer, nullable=True, default=0)
	anonymoushelpcount = db.Column(db.Integer, nullable=True, default=0)
	devicename = db.Column(db.String(128), nullable=True)

	def as_dict(self):
		return {c.name: getattr(self, c.name) for c in self.__table__.columns}

from datetime import datetime, timezone
from zoneinfo import ZoneInfo


class CalendarClaim(db.Model):

    __tablename__ = "calendar_claim"

    id = db.Column(
        db.Integer,
        primary_key=True
    )

    player_id = db.Column(
        db.String,
        nullable=False
    )

    calendar_id = db.Column(
        db.String,
        nullable=False
    )

    day = db.Column(
        db.Integer,
        nullable=False
    )

    claimed_at = db.Column(
        db.DateTime,
        nullable=False
    )

    __table_args__ = (
        db.UniqueConstraint(
            "player_id",
            "calendar_id",
            "day",
            name="uq_calendar_player_day"
        ),
    )

# =========================================================
# CALENDAR CONFIG
# =========================================================

CALENDAR_ID = "September2026"

CALENDAR_TIMEZONE = ZoneInfo(
    "Europe/Istanbul"
)

CALENDAR_START_LOCAL = datetime(
    2026,
    8,
    30,
    0,
    0,
    0,
    tzinfo=CALENDAR_TIMEZONE
)

CALENDAR_END_LOCAL = datetime(
    2026,
    9,
    23,
    23,
    59,
    59,
    tzinfo=CALENDAR_TIMEZONE
)

CALENDAR_DAYS = 25


# =========================================================
# CALENDAR STATE
# =========================================================

def GetCalendarState():

    now_utc = datetime.now(
        timezone.utc
    )

    now_local = now_utc.astimezone(
        CALENDAR_TIMEZONE
    )

    start_local = CALENDAR_START_LOCAL
    end_local = CALENDAR_END_LOCAL

    # -----------------------------------------------------
    # NOT STARTED
    # -----------------------------------------------------

    if now_local < start_local:

        return {
            "active": False,
            "started": False,
            "ended": False,
            "day": 0,

            "server_time":
                now_utc.isoformat(),

            "local_time":
                now_local.isoformat()
        }

    # -----------------------------------------------------
    # ENDED
    # -----------------------------------------------------

    if now_local > end_local:

        return {
            "active": False,
            "started": True,
            "ended": True,
            "day": CALENDAR_DAYS,

            "server_time":
                now_utc.isoformat(),

            "local_time":
                now_local.isoformat()
        }

    # -----------------------------------------------------
    # CURRENT DAY
    # -----------------------------------------------------

    day = (
        now_local.date()
        - start_local.date()
    ).days + 1

    if day < 1:
        day = 1

    if day > CALENDAR_DAYS:
        day = CALENDAR_DAYS

    return {
        "active": True,
        "started": True,
        "ended": False,
        "day": day,

        "server_time":
            now_utc.isoformat(),

        "local_time":
            now_local.isoformat()
    }

class MonthlyStoreRotation(db.Model):
    __tablename__ = "monthly_store_rotation"

    id = db.Column(
        db.Integer,
        primary_key=True
    )

    store_type = db.Column(
        db.String(32),
        nullable=False
    )

    month_key = db.Column(
        db.String(7),
        nullable=False
    )

    offer_id = db.Column(
        db.String(255),
        nullable=False
    )

    cycle = db.Column(
        db.Integer,
        nullable=False,
        default=1
    )

    created_at = db.Column(
        db.DateTime(timezone=True),
        nullable=False,
        default=lambda: datetime.now(timezone.utc)
    )

    __table_args__ = (
        db.UniqueConstraint(
            "store_type",
            "month_key",
            "offer_id",
            name="uq_monthly_store_offer"
        ),
    )

class MonthlyStorePurchase(db.Model):
    __tablename__ = "monthly_store_purchase"

    id = db.Column(
        db.Integer,
        primary_key=True
    )

    player_id = db.Column(
        db.String(255),
        nullable=False
    )

    store_type = db.Column(
        db.String(32),
        nullable=False
    )

    offer_id = db.Column(
        db.String(255),
        nullable=False
    )

    month_key = db.Column(
        db.String(7),
        nullable=False
    )

    cycle = db.Column(
        db.Integer,
        nullable=False
    )

    buy_count = db.Column(
        db.Integer,
        nullable=False,
        default=0
    )

    created_at = db.Column(
        db.DateTime(timezone=True),
        nullable=False,
        default=lambda: datetime.now(timezone.utc)
    )

    updated_at = db.Column(
        db.DateTime(timezone=True),
        nullable=False,
        default=lambda: datetime.now(timezone.utc),
        onupdate=lambda: datetime.now(timezone.utc)
    )

    __table_args__ = (
        db.UniqueConstraint(
            "player_id",
            "store_type",
            "offer_id",
            "month_key",
            "cycle",
            name="uq_monthly_store_purchase"
        ),
    )

@app.route("/")
def Index():
	return "200 App server running"

@app.route("/persist/static/manifest.json")
def Manifest():
	with open("data/persist/manifest.json", "r") as f:
		return f.read()

@app.route("/persist/static/Blueprints/<path:filename>", methods=['GET'])
def get_blueprints(filename):
    file_path = os.path.join("data/persist/blueprints", filename)

    with open(file_path, "r") as file:
        return file.read()

#only works in v1.18.0
@app.route("/persist/static/blueprints", methods=['GET'])
def Blueprints():
	data = []
	for root, dirs, files in os.walk("data/persist/blueprints"):
		for file in files:
			data.append({
				"name": file.replace(".json", ""),
				"data": open(f"{root}/{file}", "r").read()
			})
	return jsonify(data)

def get_monthly_store_offers(
    store_type,
    blueprint_filename,
    offer_count=6
):
    current_time = datetime.now(timezone.utc)

    month_key = (
        f"{current_time.year}-"
        f"{current_time.month:02d}"
    )

    # ============================================================
    # BLUEPRINT PATH
    # ============================================================

    blueprint_path = os.path.join(
        "data",
        "persist",
        "blueprints",
        blueprint_filename
    )

    print("========================================")
    print("[Monthly Store] Blueprint Debug")
    print("store_type:", store_type)
    print("blueprint_filename:", blueprint_filename)
    print("blueprint_path:", blueprint_path)
    print("absolute_path:", os.path.abspath(blueprint_path))
    print("exists:", os.path.exists(blueprint_path))
    print("========================================")

    if not os.path.exists(blueprint_path):
        raise FileNotFoundError(
            f"Blueprint not found: {blueprint_path}"
        )

    with open(
        blueprint_path,
        "r",
        encoding="utf-8"
    ) as f:
        blueprint_data = json.load(f)

    # ============================================================
    # BLUEPRINT ENTRIES
    # ============================================================

    if isinstance(blueprint_data, list):
        entries = blueprint_data

    elif isinstance(blueprint_data, dict):
        entries = blueprint_data.get(
            "data",
            []
        )

    else:
        raise ValueError(
            f"Invalid blueprint format: {blueprint_path}"
        )

    monthly_ids = []
    special_ids = []

    # ============================================================
    # MONTHLY / SPECIAL AYIR
    # ============================================================

    for entry in entries:

        if not isinstance(entry, dict):
            continue

        offer_id = entry.get("ID")

        if not offer_id:
            continue

        raw_special = entry.get(
            "IsSpecial",
            False
        )

        if isinstance(raw_special, bool):
            is_special = raw_special

        elif isinstance(raw_special, str):
            is_special = (
                raw_special.strip().lower()
                == "true"
            )

        else:
            is_special = False

        if is_special:
            special_ids.append(
                offer_id
            )
        else:
            monthly_ids.append(
                offer_id
            )

    monthly_ids = list(
        dict.fromkeys(monthly_ids)
    )

    special_ids = list(
        dict.fromkeys(special_ids)
    )

    # ============================================================
    # EN AZ 4 MONTHLY
    # ============================================================

    if len(monthly_ids) < offer_count:

        raise ValueError(
            f"[Monthly Store] "
            f"{store_type} has only "
            f"{len(monthly_ids)} monthly offers. "
            f"Required: {offer_count}"
        )

    # ============================================================
    # BU AY SEÇİLMİŞ Mİ?
    # ============================================================

    existing = (
        MonthlyStoreRotation.query
        .filter_by(
            store_type=store_type,
            month_key=month_key
        )
        .order_by(
            MonthlyStoreRotation.id.asc()
        )
        .all()
    )

    if len(existing) >= offer_count:

        monthly_result = [
            row.offer_id
            for row in existing[:offer_count]
        ]

        return {
            "monthly": monthly_result,
            "special": special_ids
        }

    # ============================================================
    # DAHA ÖNCE KULLANILANLAR
    # ============================================================

    used_rows = (
        MonthlyStoreRotation.query
        .filter_by(
            store_type=store_type
        )
        .all()
    )

    used_ids = {
        row.offer_id
        for row in used_rows
    }

    unused_ids = [
        offer_id
        for offer_id in monthly_ids
        if offer_id not in used_ids
    ]

    # ============================================================
    # CYCLE
    # ============================================================

    if len(unused_ids) >= offer_count:

        selected_ids = random.sample(
            unused_ids,
            offer_count
        )

        cycle = (
            max(
                [row.cycle for row in used_rows],
                default=1
            )
        )

    else:

        selected_ids = random.sample(
            monthly_ids,
            offer_count
        )

        cycle = (
            max(
                [row.cycle for row in used_rows],
                default=0
            ) + 1
        )

    # ============================================================
    # DB
    # ============================================================

    for offer_id in selected_ids:

        db.session.add(
            MonthlyStoreRotation(
                store_type=store_type,
                month_key=month_key,
                offer_id=offer_id,
                cycle=cycle
            )
        )

    db.session.commit()

    print(
        f"[Monthly Store] {store_type} / {month_key}"
    )

    print(
        f"[Monthly Store] Monthly: {selected_ids}"
    )

    print(
        f"[Monthly Store] Special: {special_ids}"
    )

    return {
        "monthly": selected_ids,
        "special": special_ids
    }

MONTHLY_STORE_BUY_LIMITS = {
    "evo": 1,
    "action": 3
}

def is_monthly_store_special(
    store_type,
    offer_id
):
    """
    Blueprint'ten offer'ın IsSpecial değerini kontrol eder.
    """

    blueprint_filename = {
        "evo": "db_StoreBuyEvoMaterials.json",
        "action": "db_StoreBuyActionCards.json"
    }.get(store_type)

    if not blueprint_filename:
        return False

    blueprint_path = os.path.join(
        "data",
        "persist",
        "blueprints",
        blueprint_filename
    )

    if not os.path.exists(blueprint_path):
        print(
            "[Monthly Store Special] "
            "Blueprint not found:",
            blueprint_path
        )
        return False

    try:
        with open(
            blueprint_path,
            "r",
            encoding="utf-8"
        ) as f:
            blueprint_data = json.load(f)

        # Blueprint listesi
        if isinstance(blueprint_data, list):
            entries = blueprint_data

        # Blueprint dictionary ise
        elif isinstance(blueprint_data, dict):
            entries = blueprint_data.get(
                "data",
                []
            )

        else:
            return False

        for entry in entries:

            if not isinstance(entry, dict):
                continue

            if entry.get("ID") != offer_id:
                continue

            raw_special = entry.get(
                "IsSpecial",
                False
            )

            if isinstance(raw_special, bool):
                return raw_special

            if isinstance(raw_special, str):
                return (
                    raw_special.strip().lower()
                    == "true"
                )

            return False

    except Exception as e:

        print(
            "[Monthly Store Special] "
            "ERROR:",
            str(e)
        )

    return False

def get_monthly_store_buy_count(
    player_id,
    store_type,
    offer_id
):
    current_time = datetime.now(timezone.utc)

    month_key = (
        f"{current_time.year}-"
        f"{current_time.month:02d}"
    )

    # ============================================================
    # SPECIAL CHECK
    # ============================================================

    is_special = is_monthly_store_special(
        store_type,
        offer_id
    )

    print(
        "[Monthly Store Count] "
        "is_special:",
        is_special
    )

    # ============================================================
    # SPECIAL OFFER
    # ============================================================

    if is_special:

        purchase = (
            MonthlyStorePurchase.query
            .filter_by(
                player_id=player_id,
                store_type=store_type,
                offer_id=offer_id,
                month_key=month_key,
                cycle=0
            )
            .first()
        )

        if purchase is None:
            return 0

        return purchase.buy_count

    # ============================================================
    # NORMAL OFFER
    # ============================================================

    rotation = (
        MonthlyStoreRotation.query
        .filter_by(
            store_type=store_type,
            month_key=month_key,
            offer_id=offer_id
        )
        .order_by(
            MonthlyStoreRotation.id.desc()
        )
        .first()
    )

    if rotation is None:
        return 0

    purchase = (
        MonthlyStorePurchase.query
        .filter_by(
            player_id=player_id,
            store_type=store_type,
            offer_id=offer_id,
            month_key=month_key,
            cycle=rotation.cycle
        )
        .first()
    )

    if purchase is None:
        return 0

    return purchase.buy_count

def get_monthly_store_buy_counts(
    player_id,
    store_type,
    offer_ids
):
    result = {}

    for offer_id in offer_ids:
        result[offer_id] = get_monthly_store_buy_count(
            player_id,
            store_type,
            offer_id
        )

    return result

def increment_monthly_store_buy_count(
    player_id,
    store_type,
    offer_id
):
    current_time = datetime.now(timezone.utc)

    month_key = (
        f"{current_time.year}-"
        f"{current_time.month:02d}"
    )

    is_special = is_monthly_store_special(
        store_type,
        offer_id
    )

    print("========================================")
    print("[Monthly Store Count] INCREMENT START")
    print("[Monthly Store Count] player_id:", player_id)
    print("[Monthly Store Count] store_type:", store_type)
    print("[Monthly Store Count] offer_id:", offer_id)
    print("[Monthly Store Count] month_key:", month_key)
    print("========================================")

    # ============================================================
    # SPECIAL OFFER
    # ============================================================

    if is_special:

        print(
            "[Monthly Store Count] "
            "SPECIAL OFFER"
        )

        buy_limit = MONTHLY_STORE_BUY_LIMITS.get(
            store_type,
            1
        )

        purchase = (
            MonthlyStorePurchase.query
            .filter_by(
                player_id=player_id,
                store_type=store_type,
                offer_id=offer_id,
                month_key=month_key,
                cycle=0
            )
            .first()
        )

        print(
            "[Monthly Store Count] "
            "SPECIAL purchase:",
            purchase
        )

        if purchase is not None:

            if purchase.buy_count >= buy_limit:

                print(
                    "[Monthly Store Count] "
                    "SPECIAL BUY_LIMIT_REACHED"
                )

                return {
                    "success": False,
                    "error": "BUY_LIMIT_REACHED",
                    "buy_count": purchase.buy_count,
                    "buy_limit": buy_limit
                }

            purchase.buy_count += 1

        else:

            purchase = MonthlyStorePurchase(
                player_id=player_id,
                store_type=store_type,
                offer_id=offer_id,
                month_key=month_key,
                cycle=0,
                buy_count=1
            )

            db.session.add(purchase)

        db.session.commit()

        print(
            "[Monthly Store Count] "
            "SPECIAL COMMIT OK"
        )

        return {
            "success": True,
            "buy_count": purchase.buy_count,
            "buy_limit": buy_limit
        }

    rotation = (
        MonthlyStoreRotation.query
        .filter_by(
            store_type=store_type,
            month_key=month_key,
            offer_id=offer_id
        )
        .order_by(
            MonthlyStoreRotation.id.desc()
        )
        .first()
    )

    print(
        "[Monthly Store Count] rotation:",
        rotation
    )

    if rotation is None:
        print(
            "[Monthly Store Count] ERROR: "
            "ROTATION NOT FOUND"
        )

        return {
            "success": False,
            "error": "OFFER_NOT_AVAILABLE"
        }

    print(
        "[Monthly Store Count] rotation.id:",
        rotation.id
    )

    print(
        "[Monthly Store Count] rotation.cycle:",
        rotation.cycle
    )

    buy_limit = MONTHLY_STORE_BUY_LIMITS.get(
        store_type,
        1
    )

    print(
        "[Monthly Store Count] buy_limit:",
        buy_limit
    )

    purchase = (
        MonthlyStorePurchase.query
        .filter_by(
            player_id=player_id,
            store_type=store_type,
            offer_id=offer_id,
            month_key=month_key,
            cycle=rotation.cycle
        )
        .first()
    )

    print(
        "[Monthly Store Count] existing purchase:",
        purchase
    )

    if purchase is not None:

        print(
            "[Monthly Store Count] "
            "EXISTING buy_count:",
            purchase.buy_count
        )

        if purchase.buy_count >= buy_limit:

            print(
                "[Monthly Store Count] "
                "BUY_LIMIT_REACHED:",
                purchase.buy_count,
                "/",
                buy_limit
            )

            return {
                "success": False,
                "error": "BUY_LIMIT_REACHED",
                "buy_count": purchase.buy_count,
                "buy_limit": buy_limit
            }

        purchase.buy_count += 1

        print(
            "[Monthly Store Count] "
            "INCREMENTED TO:",
            purchase.buy_count
        )

    else:

        print(
            "[Monthly Store Count] "
            "CREATING NEW PURCHASE"
        )

        purchase = MonthlyStorePurchase(
            player_id=player_id,
            store_type=store_type,
            offer_id=offer_id,
            month_key=month_key,
            cycle=rotation.cycle,
            buy_count=1
        )

        db.session.add(purchase)

        print(
            "[Monthly Store Count] "
            "NEW buy_count: 1"
        )

    db.session.commit()

    print(
        "[Monthly Store Count] COMMIT OK"
    )

    # ============================================================
    # DB'DEN TEKRAR OKU
    # ============================================================

    verify = (
        MonthlyStorePurchase.query
        .filter_by(
            player_id=player_id,
            store_type=store_type,
            offer_id=offer_id,
            month_key=month_key,
            cycle=rotation.cycle
        )
        .first()
    )

    print(
        "[Monthly Store Count] "
        "VERIFY DB:",
        verify
    )

    if verify is not None:
        print(
            "[Monthly Store Count] "
            "VERIFY buy_count:",
            verify.buy_count
        )

    print("========================================")
    print("[Monthly Store Count] INCREMENT END")
    print("========================================")

    return {
        "success": True,
        "buy_count": purchase.buy_count,
        "buy_limit": buy_limit
    }

@app.route(
    "/persist/static/monthly_store",
    methods=["GET"]
)
def GetMonthlyStore():

    try:

        # ============================================================
        # PLAYER
        # ============================================================

        player_id = request.args.get("player_id")

        if player_id is None:
            return jsonify({
                "success": False,
                "error": "MISSING_PLAYER_ID"
            }), 400

        # ============================================================
        # GET ROTATION
        # ============================================================

        evo = get_monthly_store_offers(
            "evo",
            "db_StoreBuyEvoMaterials.json",
            4
        )

        action = get_monthly_store_offers(
            "action",
            "db_StoreBuyActionCards.json",
            6
        )

        # ============================================================
        # BUY COUNTS
        # ============================================================

        action_offer_ids = list(
            dict.fromkeys(
                action["monthly"] +
                action["special"]
            )
        )

        evo_offer_ids = list(
            dict.fromkeys(
                evo["monthly"] +
                evo["special"]
            )
        )

        action_buy_counts = (
            get_monthly_store_buy_counts(
                player_id,
                "action",
                action_offer_ids
            )
        )

        evo_buy_counts = (
            get_monthly_store_buy_counts(
                player_id,
                "evo",
                evo_offer_ids
            )
        )

        # ============================================================
        # RESPONSE
        # ============================================================

        return jsonify({
            "success": True,

            "action_cards":
                action["monthly"],

            "action_card_special":
                action["special"],

            "evo_materials":
                evo["monthly"],

            "evo_special":
                evo["special"],

            "buy_counts": {
                "action":
                    action_buy_counts,

                "evo":
                    evo_buy_counts
            },

            "buy_limits": {
                "action":
                    MONTHLY_STORE_BUY_LIMITS["action"],

                "evo":
                    MONTHLY_STORE_BUY_LIMITS["evo"]
            }
        })

    except Exception as e:

        db.session.rollback()

        print("========================================")
        print("[Monthly Store] ERROR")
        print("Type:", type(e).__name__)
        print("Error:", str(e))
        traceback.print_exc()
        print("========================================")

        return jsonify({
            "success": False,
            "error": "SERVER_ERROR",
            "message": str(e)
        }), 500

@app.route(
    "/persist/static/monthly_store/buy",
    methods=["POST"]
)
def BuyMonthlyStore():

    print("========================================")
    print("[Monthly Store Buy] ROUTE HIT")
    print("[Monthly Store Buy] Content-Type:", request.content_type)
    print("[Monthly Store Buy] Raw Body:", request.get_data(as_text=True))
    print("[Monthly Store Buy] FORM:", request.form.to_dict())
    print("========================================")

    try:

        data = request.form.to_dict()

        print(
            "[Monthly Store Buy] Parsed data:",
            data
        )

        player_id = data.get("player_id")
        store_type = data.get("store_type")
        offer_id = data.get("offer_id")

        print(
            "[Monthly Store Buy] player_id =",
            repr(player_id)
        )

        print(
            "[Monthly Store Buy] store_type =",
            repr(store_type)
        )

        print(
            "[Monthly Store Buy] offer_id =",
            repr(offer_id)
        )

        # ---------------------------------------------------------
        # VALIDATION
        # ---------------------------------------------------------

        if not player_id:
            print("[Monthly Store Buy] -> 400 MISSING_PLAYER_ID")

            return jsonify({
                "success": False,
                "error": "MISSING_PLAYER_ID"
            }), 400

        if not store_type:
            print("[Monthly Store Buy] -> 400 MISSING_STORE_TYPE")

            return jsonify({
                "success": False,
                "error": "MISSING_STORE_TYPE"
            }), 400

        if not offer_id:
            print("[Monthly Store Buy] -> 400 MISSING_OFFER_ID")

            return jsonify({
                "success": False,
                "error": "MISSING_OFFER_ID"
            }), 400

        print(
            "[Monthly Store Buy] Validation OK"
        )

        # ---------------------------------------------------------
        # STORE TYPE
        # ---------------------------------------------------------

        buy_limit = MONTHLY_STORE_BUY_LIMITS.get(
            store_type
        )

        print(
            "[Monthly Store Buy] MONTHLY_STORE_BUY_LIMITS:",
            MONTHLY_STORE_BUY_LIMITS
        )

        print(
            "[Monthly Store Buy] buy_limit:",
            repr(buy_limit)
        )

        if buy_limit is None:

            print(
                "[Monthly Store Buy] -> 400 INVALID_STORE_TYPE:",
                repr(store_type)
            )

            return jsonify({
                "success": False,
                "error": "INVALID_STORE_TYPE"
            }), 400

        # ---------------------------------------------------------
        # CURRENT COUNT
        # ---------------------------------------------------------

        print(
            "[Monthly Store Buy] Calling "
            "get_monthly_store_buy_count..."
        )

        current_count = get_monthly_store_buy_count(
            player_id,
            store_type,
            offer_id
        )

        print(
            "[Monthly Store Buy] Current count:",
            repr(current_count)
        )

        print(
            "[Monthly Store Buy] Limit:",
            repr(buy_limit)
        )

        # ---------------------------------------------------------
        # LIMIT
        # ---------------------------------------------------------

        if current_count >= buy_limit:

            print(
                "[Monthly Store Buy] -> 400 "
                "BUY_LIMIT_REACHED"
            )

            return jsonify({
                "success": False,
                "error": "BUY_LIMIT_REACHED",
                "buy_count": current_count,
                "buy_limit": buy_limit
            }), 400

        # ---------------------------------------------------------
        # INCREMENT
        # ---------------------------------------------------------

        print(
            "[Monthly Store Buy] Calling "
            "increment_monthly_store_buy_count..."
        )

        result = increment_monthly_store_buy_count(
            player_id,
            store_type,
            offer_id
        )

        print(
            "[Monthly Store Buy] Increment result:",
            repr(result)
        )

        if not result.get("success"):

            print(
                "[Monthly Store Buy] -> 400 "
                "INCREMENT FAILED"
            )

            return jsonify(result), 400

        # ---------------------------------------------------------
        # SUCCESS
        # ---------------------------------------------------------

        print(
            "[Monthly Store Buy] SUCCESS"
        )

        return jsonify({
            "success": True,
            "store_type": store_type,
            "offer_id": offer_id,
            "buy_count": result["buy_count"],
            "buy_limit": result["buy_limit"]
        }), 200

    except Exception as e:

        db.session.rollback()

        import traceback

        print("========================================")
        print("[Monthly Store Buy] EXCEPTION")
        print("Type:", type(e).__name__)
        print("Error:", str(e))
        traceback.print_exc()
        print("========================================")

        return jsonify({
            "success": False,
            "error": "SERVER_ERROR",
            "message": str(e)
        }), 500

@app.route("/calendar/info", methods=["GET"])
def CalendarInfo():

    try:
        player_id = request.args.get("player_id")

        state = GetCalendarState()

        response = {
            "success": True,

            "calendar_id": CALENDAR_ID,

            "active": state["active"],
            "started": state["started"],
            "ended": state["ended"],

            "current_day": state["day"],
            "total_days": CALENDAR_DAYS,

            "server_time": state["server_time"],
            "local_time": state["local_time"],

            "timezone": "Europe/Istanbul",

            "start": CALENDAR_START_LOCAL.isoformat(),
            "end": CALENDAR_END_LOCAL.isoformat(),

            "claimed_days": []
        }

        # ---------------------------------------------------------
        # PLAYER CLAIMS
        # ---------------------------------------------------------

        if player_id:

            player = Player.query.filter_by(
                username=player_id
            ).first()

            if player:

                claims = CalendarClaim.query.filter_by(
                    player_id=player.username,
                    calendar_id=CALENDAR_ID
                ).order_by(
                    CalendarClaim.day.asc()
                ).all()

                response["claimed_days"] = [
                    claim.day
                    for claim in claims
                ]

        # ---------------------------------------------------------
        # CAN CLAIM
        # ---------------------------------------------------------

        if (
            state["active"]
            and state["started"]
            and not state["ended"]
            and state["day"] > 0
        ):

            if player_id:

                already_claimed = CalendarClaim.query.filter_by(
                    player_id=player_id,
                    calendar_id=CALENDAR_ID,
                    day=state["day"]
                ).first()

                response["can_claim"] = (
                    already_claimed is None
                )

            else:

                response["can_claim"] = False

        else:

            response["can_claim"] = False

        return jsonify(response), 200

    except Exception as e:

        print(
            "[Calendar] INFO ERROR:",
            repr(e)
        )

        return jsonify({
            "success": False,
            "error": "SERVER_ERROR"
        }), 500

@app.route("/calendar/claim", methods=["POST"])
def CalendarClaimReward():

    try:
        payload = request.get_json(silent=True)

        if not payload:
            return jsonify({
                "success": False,
                "error": "JSON body required"
            }), 400

        player_id = payload.get("player_id")

        if not player_id:
            return jsonify({
                "success": False,
                "error": "player_id is required"
            }), 400

        # ---------------------------------------------------------
        # PLAYER
        # ---------------------------------------------------------

        player = Player.query.filter_by(
            username=player_id
        ).first()

        if player is None:
            return jsonify({
                "success": False,
                "error": "PLAYER_NOT_FOUND"
            }), 404

        # ---------------------------------------------------------
        # SERVER DATE
        # ---------------------------------------------------------

        state = GetCalendarState()

        if not state["started"]:
            return jsonify({
                "success": False,
                "error": "CALENDAR_NOT_STARTED",
                "server_time": state["server_time"]
            }), 400

        if state["ended"]:
            return jsonify({
                "success": False,
                "error": "CALENDAR_ENDED",
                "server_time": state["server_time"]
            }), 400

        current_day = state["day"]

        # ---------------------------------------------------------
        # IMPORTANT:
        # Client does NOT choose the day.
        # ---------------------------------------------------------

        # ---------------------------------------------------------
        # ALREADY CLAIMED?
        # ---------------------------------------------------------

        existing_claim = CalendarClaim.query.filter_by(
            player_id=player.username,
            calendar_id=CALENDAR_ID,
            day=current_day
        ).first()

        if existing_claim is not None:

            return jsonify({
                "success": False,
                "error": "ALREADY_CLAIMED",
                "day": current_day,
                "claimed_at": existing_claim.claimed_at.isoformat()
            }), 400

        # ---------------------------------------------------------
        # GET REWARD
        # ---------------------------------------------------------

        reward = GetCalendarReward(
            current_day
        )

        if reward is None:
            return jsonify({
                "success": False,
                "error": "REWARD_NOT_CONFIGURED",
                "day": current_day
            }), 500

        # ---------------------------------------------------------
        # GRANT REWARD
        # ---------------------------------------------------------

        result = GrantCalendarReward(
            player,
            reward
        )

        if not result["success"]:
            db.session.rollback()

            return jsonify(result), 400

        claim = CalendarClaim(
            player_id=player.username,
            calendar_id=CALENDAR_ID,
            day=current_day,
            claimed_at=datetime.now(timezone.utc)
        )

        db.session.add(claim)

        try:

            db.session.commit()

        except Exception as e:

            db.session.rollback()

            print(
                "[Calendar] CLAIM COMMIT ERROR:",
                repr(e)
            )

            return jsonify({
                "success": False,
                "error": "CLAIM_SAVE_FAILED"
            }), 500

        # ---------------------------------------------------------
        # SUCCESS
        # ---------------------------------------------------------

        return jsonify({
            "success": True,

            "calendar_id": CALENDAR_ID,

            "day": current_day,

            "reward": reward,

            "server_time": state["server_time"],

            "message": result.get(
                "message",
                "Calendar reward claimed."
            )
        }), 200

    except Exception as e:

        db.session.rollback()

        print(
            "[Calendar] ERROR:",
            repr(e)
        )

        return jsonify({
            "success": False,
            "error": "SERVER_ERROR"
        }), 500

def GrantCalendarReward(player, reward):

    reward_type = reward.get("type")
    quantity = int(reward.get("quantity", 0))
    gift_id = reward.get("gift_id", "")

    if quantity <= 0:
        return {
            "success": False,
            "error": "INVALID_REWARD"
        }

    game_field = player.game

    save_json = None

    if game_field:

        game_text = None

        if isinstance(
            game_field,
            (bytes, bytearray)
        ):
            try:
                game_text = game_field.decode(
                    "utf-8"
                )
            except Exception:
                game_text = None

        else:
            game_text = game_field

        # Raw JSON
        if (
            isinstance(game_text, str)
            and game_text.strip().startswith("{")
        ):
            try:
                save_json = json.loads(
                    game_text
                )
            except Exception:
                save_json = None

        # Encrypted save
        if save_json is None:

            try:
                decrypted = DecryptGameData(
                    game_field
                )

                if isinstance(
                    decrypted,
                    dict
                ):
                    save_json = decrypted

                elif isinstance(
                    decrypted,
                    str
                ):
                    save_json = json.loads(
                        decrypted
                    )

            except Exception:
                save_json = None

    if save_json is None:
        return {
            "success": False,
            "error": "INVALID_PLAYER_SAVE"
        }

    # ---------------------------------------------------------
    # HARD CURRENCY
    # ---------------------------------------------------------

    if reward_type == "HardCurrency":

        old_amount = int(
            save_json.get(
                "FreeHardCurrency",
                0
            ) or 0
        )

        new_amount = (
            old_amount + quantity
        )

        save_json[
            "FreeHardCurrency"
        ] = new_amount

        player.game = json.dumps(
            save_json,
            ensure_ascii=False
        )

        return {
            "success": True,
            "message": (
                f"+{quantity} Hard Currency"
            ),
            "reward": {
                "type": reward_type,
                "quantity": quantity
            }
        }

    # ---------------------------------------------------------
    # SOFT CURRENCY
    # ---------------------------------------------------------

    if reward_type == "SoftCurrency":

        old_amount = int(
            save_json.get(
                "SoftCurrency",
                0
            ) or 0
        )

        new_amount = (
            old_amount + quantity
        )

        save_json[
            "SoftCurrency"
        ] = new_amount

        player.game = json.dumps(
            save_json,
            ensure_ascii=False
        )

        return {
            "success": True,
            "message": (
                f"+{quantity} Soft Currency"
            ),
            "reward": {
                "type": reward_type,
                "quantity": quantity
            }
        }

    return {
        "success": False,
        "error": (
            "UNSUPPORTED_REWARD_TYPE"
        )
    }


# ============================================================
# GIFT CODES
# ============================================================

GIFT_CODES_PATH = "data/persist/gift_codes.json"
GIFT_CODES_LOCK = threading.Lock()


def EnsureGiftCodesFile():
    os.makedirs(
        os.path.dirname(GIFT_CODES_PATH),
        exist_ok=True
    )

    if not os.path.exists(GIFT_CODES_PATH):
        with open(
            GIFT_CODES_PATH,
            "w",
            encoding="utf-8"
        ) as f:
            json.dump(
                {"codes": []},
                f,
                ensure_ascii=False,
                indent=4
            )


def LoadGiftCodes():
    EnsureGiftCodesFile()

    try:
        with open(
            GIFT_CODES_PATH,
            "r",
            encoding="utf-8"
        ) as f:
            data = json.load(f)

        if not isinstance(data, dict):
            return {"codes": []}

        if not isinstance(data.get("codes"), list):
            data["codes"] = []

        return data

    except Exception as e:
        Log(
            "giftcode",
            "Failed to load gift codes: " + repr(e)
        )
        return {"codes": []}


def SaveGiftCodes(data):
    EnsureGiftCodesFile()

    temporary_path = GIFT_CODES_PATH + ".tmp"

    with open(
        temporary_path,
        "w",
        encoding="utf-8"
    ) as f:
        json.dump(
            data,
            f,
            ensure_ascii=False,
            indent=4
        )

    os.replace(
        temporary_path,
        GIFT_CODES_PATH
    )


def NormalizeGiftCode(code):
    """
    Match the Unity client normalization exactly:
    whitespace, '-' and '_' are ignored and the code is upper-cased.
    """
    if code is None:
        return ""

    return re.sub(
        r"[\s\-_]",
        "",
        str(code)
    ).upper()


def FindGiftCode(data, code):
    normalized = NormalizeGiftCode(code)

    for item in data.get("codes", []):
        if NormalizeGiftCode(item.get("code")) == normalized:
            return item

    return None


def ParseGiftCodeRewardList(raw_value, field_name):
    if raw_value is None or raw_value.strip() == "":
        return []

    try:
        parsed = json.loads(raw_value)
    except Exception:
        raise ValueError(
            field_name + " must contain valid JSON."
        )

    if not isinstance(parsed, list):
        raise ValueError(
            field_name + " must be a JSON array."
        )

    return parsed


def GiftCodeIsActive(gift):
    if not bool(gift.get("enabled", True)):
        return False

    now = datetime.now(timezone.utc)

    start_date = gift.get("start_date")
    end_date = gift.get("end_date")

    if start_date:
        try:
            start = datetime.fromisoformat(
                str(start_date).replace("Z", "+00:00")
            )

            if start.tzinfo is None:
                start = start.replace(tzinfo=timezone.utc)

            if now < start:
                return False

        except ValueError:
            return False

    if end_date:
        try:
            end = datetime.fromisoformat(
                str(end_date).replace("Z", "+00:00")
            )

            if end.tzinfo is None:
                end = end.replace(tzinfo=timezone.utc)

            if now > end:
                return False

        except ValueError:
            return False

    return True


def GrantGiftCodeReward(player, gift):
    game_field = player.game

    save_json = None

    if game_field:

        game_text = None

        if isinstance(
            game_field,
            (bytes, bytearray)
        ):
            try:
                game_text = game_field.decode("utf-8")
            except Exception:
                game_text = None

        else:
            game_text = game_field

        if (
            isinstance(game_text, str)
            and game_text.strip().startswith("{")
        ):
            try:
                save_json = json.loads(game_text)
            except Exception:
                save_json = None

        if save_json is None:
            try:
                decrypted = DecryptGameData(
                    game_field
                )

                if isinstance(decrypted, dict):
                    save_json = decrypted

                elif isinstance(decrypted, str):
                    save_json = json.loads(decrypted)

            except Exception:
                save_json = None

    if save_json is None:
        return {
            "success": False,
            "error": "INVALID_PLAYER_SAVE"
        }

    rewards = gift.get("rewards", {})

    if not isinstance(rewards, dict):
        return {
            "success": False,
            "error": "INVALID_REWARDS"
        }

    granted = {
        "soft_currency": 0,
        "free_hard_currency": 0,
        "paid_hard_currency": 0,
        "creatures": [],
        "action_cards": []
    }

    # --------------------------------------------------------
    # CURRENCIES
    # --------------------------------------------------------

    soft_currency = int(
        rewards.get("soft_currency", 0) or 0
    )

    free_hard_currency = int(
        rewards.get("free_hard_currency", 0) or 0
    )

    paid_hard_currency = int(
        rewards.get("paid_hard_currency", 0) or 0
    )

    if soft_currency < 0:
        return {
            "success": False,
            "error": "INVALID_SOFT_CURRENCY"
        }

    if free_hard_currency < 0:
        return {
            "success": False,
            "error": "INVALID_FREE_HARD_CURRENCY"
        }

    if paid_hard_currency < 0:
        return {
            "success": False,
            "error": "INVALID_PAID_HARD_CURRENCY"
        }

    save_json["SoftCurrency"] = (
        int(save_json.get("SoftCurrency", 0) or 0)
        + soft_currency
    )

    save_json["FreeHardCurrency"] = (
        int(save_json.get("FreeHardCurrency", 0) or 0)
        + free_hard_currency
    )

    save_json["PaidHardCurrency"] = (
        int(save_json.get("PaidHardCurrency", 0) or 0)
        + paid_hard_currency
    )

    granted["soft_currency"] = soft_currency
    granted["free_hard_currency"] = free_hard_currency
    granted["paid_hard_currency"] = paid_hard_currency

    # --------------------------------------------------------
    # INVENTORY
    # --------------------------------------------------------

    inventory = save_json.get("Inventory")

    if inventory is None:
        inventory = []
        save_json["Inventory"] = inventory

    if not isinstance(inventory, list):
        return {
            "success": False,
            "error": "INVALID_INVENTORY"
        }

    max_uid = 0

    for item in inventory:
        if not isinstance(item, dict):
            continue

        try:
            uid = int(item.get("UniqueID", 0))
            if uid > max_uid:
                max_uid = uid
        except Exception:
            pass

    next_uid = max_uid + 1

    # --------------------------------------------------------
    # CREATURES
    # --------------------------------------------------------

    creatures = rewards.get("creatures", [])

    if not isinstance(creatures, list):
        return {
            "success": False,
            "error": "INVALID_CREATURE_REWARDS"
        }

    for reward in creatures:

        if not isinstance(reward, dict):
            return {
                "success": False,
                "error": "INVALID_CREATURE_REWARD"
            }

        creature_id = str(
            reward.get("id", "")
        ).strip()

        if not creature_id:
            return {
                "success": False,
                "error": "CREATURE_ID_REQUIRED"
            }

        amount = int(
            reward.get("amount", 1)
        )

        star_rating = int(
            reward.get("star_rating", 1)
        )

        if amount < 1 or amount > 999:
            return {
                "success": False,
                "error": "INVALID_CREATURE_AMOUNT"
            }

        if star_rating < 1 or star_rating > 6:
            return {
                "success": False,
                "error": "INVALID_CREATURE_STAR_RATING"
            }

        for _ in range(amount):

            inventory.append({
                "_T": "CR",
                "ID": creature_id,
                "UniqueID": int(next_uid),
                "Xp": 0,
                "Favorite": 0,
                "Passive": 1,
                "PassiveFeeds": 0,
                "StarRating": star_rating
            })

            next_uid += 1

        granted["creatures"].append({
            "id": creature_id,
            "amount": amount,
            "star_rating": star_rating
        })

    # --------------------------------------------------------
    # ACTION CARDS / EX CARDS
    # --------------------------------------------------------

    action_cards = rewards.get(
        "action_cards",
        []
    )

    if not isinstance(action_cards, list):
        return {
            "success": False,
            "error": "INVALID_ACTION_CARD_REWARDS"
        }

    for reward in action_cards:

        if not isinstance(reward, dict):
            return {
                "success": False,
                "error": "INVALID_ACTION_CARD_REWARD"
            }

        card_id = str(
            reward.get("id", "")
        ).strip()

        if not card_id:
            return {
                "success": False,
                "error": "ACTION_CARD_ID_REQUIRED"
            }

        amount = int(
            reward.get("amount", 1)
        )

        if amount < 1 or amount > 999:
            return {
                "success": False,
                "error": "INVALID_ACTION_CARD_AMOUNT"
            }

        # ExCard inventory entries use the same
        # UniqueID system as creature entries.
        for _ in range(amount):

            inventory.append({
                "_T": "EX",
                "ID": card_id,
                "UniqueID": int(next_uid),
                "Favorite": 0
            })

            next_uid += 1

        granted["action_cards"].append({
            "id": card_id,
            "amount": amount
        })

    player.game = json.dumps(
        save_json,
        ensure_ascii=False
    )

    return {
        "success": True,
        "reward": granted
    }


def GiftCodeAdminAllowed():
    if not current_user.is_authenticated:
        return False

    if not isAdmin(current_user):
        return False

    return int(getattr(current_user, "rank", 999)) == 0


@app.route(
    "/admin/gift-codes",
    methods=["GET"]
)
@login_required
def AdminGiftCodes():

    if not GiftCodeAdminAllowed():
        return abort(404)

    with GIFT_CODES_LOCK:
        data = LoadGiftCodes()

    codes = data.get("codes", [])

    for gift in codes:
        gift["used_count"] = len(
            gift.get("used_by", [])
        )

    return render_template(
        "admin_giftcodes.html",
        codes=codes
    )


@app.route(
    "/admin/gift-codes/create",
    methods=["GET", "POST"]
)
@login_required
def AdminGiftCodeCreate():

    if not GiftCodeAdminAllowed():
        return abort(404)

    if request.method == "GET":
        return render_template(
            "admin_giftcode_create.html"
        )

    code = NormalizeGiftCode(
        request.form.get("code", "")
    )

    if (
        not code
        or not re.fullmatch(
            r"[A-Z0-9_-]{3,64}",
            code
        )
    ):
        return make_response(
            "Invalid gift code. Use 3-64 letters, numbers, '_' or '-'.",
            400
        )

    try:
        soft_currency = int(
            request.form.get(
                "soft_currency",
                0
            ) or 0
        )

        free_hard_currency = int(
            request.form.get(
                "free_hard_currency",
                0
            ) or 0
        )

        paid_hard_currency = int(
            request.form.get(
                "paid_hard_currency",
                0
            ) or 0
        )

        max_uses = int(
            request.form.get(
                "max_uses",
                0
            ) or 0
        )

        if (
            soft_currency < 0
            or free_hard_currency < 0
            or paid_hard_currency < 0
            or max_uses < 0
        ):
            raise ValueError()

        creatures = ParseGiftCodeRewardList(
            request.form.get(
                "creatures_json",
                ""
            ),
            "Creatures JSON"
        )

        action_cards = ParseGiftCodeRewardList(
            request.form.get(
                "action_cards_json",
                ""
            ),
            "Action Cards JSON"
        )

    except ValueError as e:

        return make_response(
            str(e)
            if str(e)
            else "Reward values are invalid.",
            400
        )

    start_date = (
        request.form.get(
            "start_date",
            ""
        ).strip()
        or None
    )

    end_date = (
        request.form.get(
            "end_date",
            ""
        ).strip()
        or None
    )

    gift = {
        "code": code,
        "enabled": True,
        "subject": request.form.get(
            "subject",
            ""
        ).strip(),
        "message": request.form.get(
            "message",
            ""
        ).strip(),
        "start_date": start_date,
        "end_date": end_date,
        "max_uses": max_uses,
        "used_by": [],
        "created_at": datetime.now(
            timezone.utc
        ).isoformat(),
        "created_by": current_user.username,
        "rewards": {
            "soft_currency": soft_currency,
            "free_hard_currency": free_hard_currency,
            "paid_hard_currency": paid_hard_currency,
            "creatures": creatures,
            "action_cards": action_cards
        }
    }

    with GIFT_CODES_LOCK:

        data = LoadGiftCodes()

        if FindGiftCode(
            data,
            code
        ) is not None:

            return make_response(
                "Gift code already exists.",
                409
            )

        data["codes"].append(gift)
        SaveGiftCodes(data)

    Log(
        "admin",
        current_user.username
        + " created gift code "
        + code
    )

    return redirect(
        "/admin/gift-codes"
    )

# ============================================================
# GIFT CODE REWARD CATALOG API
# ============================================================

@app.route(
    "/admin/gift-codes/catalog/<kind>",
    methods=["GET"]
)
@login_required
def AdminGiftCodeCatalog(kind):

    # Only Gift Code administrators can access the catalog.
    if not GiftCodeAdminAllowed():
        return abort(404)

    # --------------------------------------------------------
    # SELECT CATALOG
    # --------------------------------------------------------

    if kind == "creatures":

        catalog_path = (
            "data/persist/blueprints/"
            "db_Creatures.json"
        )

        fields = [
            "ID",
            "Name",
            "Prefab",
            "Faction",
            "Rarity"
        ]

    elif kind == "action-cards":

        catalog_path = (
            "data/persist/blueprints/"
            "db_ActionCards.json"
        )

        fields = [
            "ID",
            "Name",
            "TypeText",
            "Faction",
            "Rarity",
            "Cost"
        ]

    else:

        return jsonify({
            "success": False,
            "error": "UNKNOWN_CATALOG"
        }), 404

    # --------------------------------------------------------
    # LOAD JSON
    # --------------------------------------------------------

    try:

        with open(
            catalog_path,
            "r",
            encoding="utf-8"
        ) as f:

            raw_data = json.load(f)

    except Exception as e:

        Log(
            "giftcode",
            "Failed to load catalog "
            + catalog_path
            + ": "
            + repr(e)
        )

        return jsonify({
            "success": False,
            "error": "CATALOG_LOAD_FAILED"
        }), 500

    # --------------------------------------------------------
    # NORMALIZE CATALOG
    # --------------------------------------------------------

    if not isinstance(
        raw_data,
        list
    ):

        raw_data = []

    items = []

    for item in raw_data:

        if not isinstance(
            item,
            dict
        ):
            continue

        entry = {}

        for field in fields:

            value = item.get(
                field
            )

            if (
                value is not None
                and value != ""
            ):

                entry[field] = value

        # ID is required.
        if entry.get("ID"):

            items.append(
                entry
            )

    # --------------------------------------------------------
    # RESPONSE
    # --------------------------------------------------------

    return jsonify({
        "success": True,
        "items": items
    })

@app.route(
    "/admin/gift-codes/<code>/toggle",
    methods=["POST"]
)
@login_required
def AdminGiftCodeToggle(code):

    if not GiftCodeAdminAllowed():
        return abort(404)

    with GIFT_CODES_LOCK:

        data = LoadGiftCodes()
        gift = FindGiftCode(data, code)

        if gift is None:
            return make_response(
                "Gift code not found.",
                404
            )

        gift["enabled"] = not bool(
            gift.get("enabled", True)
        )

        SaveGiftCodes(data)

    Log(
        "admin",
        current_user.username
        + " toggled gift code "
        + NormalizeGiftCode(code)
    )

    return redirect(
        "/admin/gift-codes"
    )


@app.route(
    "/admin/gift-codes/<code>/delete",
    methods=["POST"]
)
@login_required
def AdminGiftCodeDelete(code):

    if not GiftCodeAdminAllowed():
        return abort(404)

    normalized = NormalizeGiftCode(code)

    with GIFT_CODES_LOCK:

        data = LoadGiftCodes()

        old_count = len(
            data["codes"]
        )

        data["codes"] = [
            gift
            for gift in data["codes"]
            if NormalizeGiftCode(
                gift.get("code")
            ) != normalized
        ]

        if len(data["codes"]) == old_count:
            return make_response(
                "Gift code not found.",
                404
            )

        SaveGiftCodes(data)

    Log(
        "admin",
        current_user.username
        + " deleted gift code "
        + normalized
    )

    return redirect(
        "/admin/gift-codes"
    )


@app.route(
    "/multiplayer/redeemcodeDW/",
    methods=["POST"]
)
def MultiplayerRedeemCode():
    """
    Server-side redeem endpoint used by the Unity client.

    IMPORTANT:
    The client applies the returned rewards locally through ApplyServerRedeem().
    Therefore this endpoint MUST NOT modify Player.game/inventory itself.
    It only validates/claims the code and returns the protocol expected by
    the client:
        reason
        fields
        rewards
        deliver = "now"
    """

    try:
        payload = request.get_json(silent=True)

        if not isinstance(payload, dict):
            client_data = parse_qs(
                request.get_data().decode("utf-8")
            )

            payload = {
                key: value[0] if len(value) == 1 else value
                for key, value in client_data.items()
            }

        redeem_code = NormalizeGiftCode(
            payload.get(
                "redeemcode",
                payload.get("code", "")
            )
        )

        player_id = payload.get("player_id")

        if not player_id:
            player_id = request.headers.get("Player-Id")

        if not redeem_code:
            return jsonify({
                "success": False,
                "reason": "INVALID",
                "error": "REDEEM_CODE_REQUIRED"
            }), 400

        if not player_id:
            return jsonify({
                "success": False,
                "reason": "ERROR",
                "error": "PLAYER_ID_REQUIRED"
            }), 400

        player_id = str(player_id)

        if InvalidUsername(player_id):
            return jsonify({
                "success": False,
                "reason": "ERROR",
                "error": "INVALID_USERNAME"
            }), 400

        if IsUserBanned(
            player_id,
            IPFromRequest(request)
        ):
            return jsonify({
                "success": False,
                "reason": "ERROR",
                "error": "USER_BANNED"
            }), 400

        player = Player.query.filter_by(
            username=player_id
        ).first()

        if player is None:
            return jsonify({
                "success": False,
                "reason": "ERROR",
                "error": "PLAYER_NOT_FOUND"
            }), 404

        # The JSON gift-code store is protected by one process-wide lock.
        # This prevents two simultaneous requests from claiming the same
        # code/player pair in the same Flask process.
        with GIFT_CODES_LOCK:
            data = LoadGiftCodes()

            gift = FindGiftCode(
                data,
                redeem_code
            )

            if gift is None:
                return jsonify({
                    "success": False,
                    "reason": "INVALID",
                    "error": "INVALID_REDEEM_CODE"
                }), 400

            if not bool(gift.get("enabled", True)):
                return jsonify({
                    "success": False,
                    "reason": "ERROR",
                    "error": "REDEEM_CODE_DISABLED"
                }), 400

            now = datetime.now(timezone.utc)

            start_date = gift.get("start_date")
            end_date = gift.get("end_date")

            if start_date:
                try:
                    start = datetime.fromisoformat(
                        str(start_date).replace("Z", "+00:00")
                    )

                    if start.tzinfo is None:
                        start = start.replace(
                            tzinfo=timezone.utc
                        )

                    if now < start:
                        return jsonify({
                            "success": False,
                            "reason": "NOT_STARTED",
                            "error": "REDEEM_CODE_NOT_STARTED"
                        }), 400

                except ValueError:
                    return jsonify({
                        "success": False,
                        "reason": "ERROR",
                        "error": "INVALID_START_DATE"
                    }), 400

            if end_date:
                try:
                    end = datetime.fromisoformat(
                        str(end_date).replace("Z", "+00:00")
                    )

                    if end.tzinfo is None:
                        end = end.replace(
                            tzinfo=timezone.utc
                        )

                    if now > end:
                        return jsonify({
                            "success": False,
                            "reason": "EXPIRED",
                            "error": "REDEEM_CODE_EXPIRED"
                        }), 400

                except ValueError:
                    return jsonify({
                        "success": False,
                        "reason": "ERROR",
                        "error": "INVALID_END_DATE"
                    }), 400

            used_by = gift.get("used_by", [])

            if not isinstance(used_by, list):
                used_by = []

            player_id_string = str(player.username)

            if player_id_string in used_by:
                return jsonify({
                    "success": False,
                    "reason": "ALREADY_CLAIMED",
                    "error": "ALREADY_REDEEMED"
                }), 400

            max_uses = int(
                gift.get("max_uses", 0) or 0
            )

            if max_uses > 0 and len(used_by) >= max_uses:
                return jsonify({
                    "success": False,
                    "reason": "LIMIT_REACHED",
                    "error": "REDEEM_CODE_LIMIT_REACHED"
                }), 400

            rewards_config = gift.get("rewards", {})

            if not isinstance(rewards_config, dict):
                return jsonify({
                    "success": False,
                    "reason": "ERROR",
                    "error": "INVALID_REWARDS"
                }), 400

            try:
                coins = max(
                    0,
                    int(rewards_config.get(
                        "soft_currency",
                        0
                    ) or 0)
                )

                gems = max(
                    0,
                    int(rewards_config.get(
                        "free_hard_currency",
                        0
                    ) or 0)
                )

                paid_hard_currency = int(
                    rewards_config.get(
                        "paid_hard_currency",
                        0
                    ) or 0
                )

            except (TypeError, ValueError):
                return jsonify({
                    "success": False,
                    "reason": "ERROR",
                    "error": "INVALID_CURRENCY_REWARD"
                }), 400

            # The supplied Unity ApplyServerRedeem() protocol supports
            # gems, coins, creatures and cards. It does NOT apply
            # PaidHardCurrency. Never silently discard that reward.
            if paid_hard_currency != 0:
                return jsonify({
                    "success": False,
                    "reason": "ERROR",
                    "error": "UNSUPPORTED_PAID_HARD_CURRENCY"
                }), 400

            creature_rewards = rewards_config.get(
                "creatures",
                []
            )

            card_rewards = rewards_config.get(
                "action_cards",
                []
            )

            if not isinstance(creature_rewards, list):
                return jsonify({
                    "success": False,
                    "reason": "ERROR",
                    "error": "INVALID_CREATURE_REWARDS"
                }), 400

            if not isinstance(card_rewards, list):
                return jsonify({
                    "success": False,
                    "reason": "ERROR",
                    "error": "INVALID_ACTION_CARD_REWARDS"
                }), 400

            # The current Unity client expects arrays of STRING IDs.
            # Amount is represented by repeating the same ID.
            creatures = []
            for reward in creature_rewards:
                if isinstance(reward, dict):
                    item_id = str(
                        reward.get("id", "")
                    ).strip()
                    amount = int(
                        reward.get("amount", 1) or 1
                    )
                else:
                    item_id = str(reward).strip()
                    amount = 1

                if not item_id:
                    return jsonify({
                        "success": False,
                        "reason": "ERROR",
                        "error": "CREATURE_ID_REQUIRED"
                    }), 400

                if amount < 1 or amount > 999:
                    return jsonify({
                        "success": False,
                        "reason": "ERROR",
                        "error": "INVALID_CREATURE_AMOUNT"
                    }), 400

                creatures.extend([item_id] * amount)

            cards = []
            for reward in card_rewards:
                if isinstance(reward, dict):
                    item_id = str(
                        reward.get("id", "")
                    ).strip()
                    amount = int(
                        reward.get("amount", 1) or 1
                    )
                else:
                    item_id = str(reward).strip()
                    amount = 1

                if not item_id:
                    return jsonify({
                        "success": False,
                        "reason": "ERROR",
                        "error": "ACTION_CARD_ID_REQUIRED"
                    }), 400

                if amount < 1 or amount > 999:
                    return jsonify({
                        "success": False,
                        "reason": "ERROR",
                        "error": "INVALID_ACTION_CARD_AMOUNT"
                    }), 400

                cards.extend([item_id] * amount)

            # Claim is committed to the JSON store before the response is
            # returned. The client will then add the returned rewards to
            # its local PlayerSaveData exactly once.
            used_by.append(player_id_string)
            gift["used_by"] = used_by

            SaveGiftCodes(data)

        Log(
            "redeemcode",
            player_id_string
            + " redeemed "
            + redeem_code
        )

        return jsonify({
            "success": True,
            "reason": "OK",
            "redeemcode": redeem_code,
            "subject": gift.get(
                "subject",
                ""
            ),
            "message": gift.get(
                "message",
                ""
            ),
            "fields": {},
            "rewards": {
                "gems": gems,
                "coins": coins,
                "creatures": creatures,
                "cards": cards
            },
            "deliver": "now"
        }), 200

    except Exception as e:
        db.session.rollback()

        Log(
            "redeemcode",
            "Redeem error: " + repr(e)
        )

        return jsonify({
            "success": False,
            "reason": "ERROR",
            "error": "SERVER_ERROR"
        }), 500


@app.route("/persist/static/pvp_banlist", methods=['GET'])
def GetPVPBanlist():
	"""
	Returns the current PVP banlist based on SERVER time.

	Returns JSON:
	{
		"banned_creatures": ["CreatureID1", "CreatureID2", ...],
		"banned_leaders": ["LeaderID1", "LeaderID2", ...],
		"server_time": "2025-03-15T14:30:00Z",
		"active_bans": [...]
	}
	"""
	try:
		# Read banlist blueprint
		banlist_path = "data/persist/blueprints/db_PVPBanlist.json"

		if not os.path.exists(banlist_path):
			# If file doesn't exist yet, return empty bans
			return jsonify({
				"banned_creatures": [],
				"banned_leaders": [],
				"server_time": datetime.now(timezone.utc).isoformat(),
				"active_bans": [],
				"info": "Banlist file not found - no bans active"
			})

		with open(banlist_path, "r", encoding='utf-8') as f:
			banlist_data = json.load(f)

		# Get current SERVER time (UTC) - this can't be faked by clients!
		current_time = datetime.now(timezone.utc)

		# Collect all active bans
		banned_creatures = []
		banned_leaders = []
		active_bans_info = []

		for season in banlist_data.get("seasons", []):
			try:
				# Parse start and end dates
				start_date_str = season.get("start_date", "")
				end_date_str = season.get("end_date", "")

				# Handle ISO 8601 format with Z suffix
				start_date = datetime.fromisoformat(start_date_str.replace("Z", "+00:00"))
				end_date = datetime.fromisoformat(end_date_str.replace("Z", "+00:00"))

				# Check if this ban period is currently active
				if start_date <= current_time <= end_date:
					# Add creatures from this active ban period
					for creature_id in season.get("banned_creatures", []):
						if creature_id not in banned_creatures:
							banned_creatures.append(creature_id)

					# Add leaders from this active ban period
					for leader_id in season.get("banned_leaders", []):
						if leader_id not in banned_leaders:
							banned_leaders.append(leader_id)

					# Track which bans are active
					active_bans_info.append({
						"id": season.get("id", "unknown"),
						"name": season.get("name", "Unnamed Ban Period"),
						"starts": start_date_str,
						"expires": end_date_str
					})

			except (KeyError, ValueError) as e:
				# Skip malformed ban entries
				print(f"[PVP Banlist] Warning: Skipping malformed ban entry: {e}")
				continue

		# Return combined banlist
		return jsonify({
			"banned_creatures": banned_creatures,
			"banned_leaders": banned_leaders,
			"server_time": current_time.isoformat(),
			"active_bans": active_bans_info
		})

	except Exception as e:
		# Log error but return empty bans to not break PVP
		print(f"[PVP Banlist] Error loading banlist: {e}")
		return jsonify({
			"banned_creatures": [],
			"banned_leaders": [],
			"server_time": datetime.now(timezone.utc).isoformat(),
			"active_bans": [],
			"error": str(e)
		})

@app.route("/persist/messages_received_ids")
def PersistMessagesReceivedIDs():
	return send_from_directory(directory="", path="data/persist/messages_received_ids.json", as_attachment=True, download_name="messages_received_ids.json")

@app.route("/persist/messages_get/<string:message>")
def PersistMessagesGet(message):
    #check if message exists
	if not os.path.exists(f"data/persist/messages/{message}.json"):
		return make_response("Message not found!", 404)
	return send_from_directory(directory="", path=f"data/persist/messages/{message}.json", as_attachment=True, download_name=f"{message}.json")

@app.route("/time/")
def Time():
    data = {
        "data": {
            "server_time": datetime.now(timezone.utc).strftime('%a, %d %b %Y %H:%M:%S GMT'),
        }
    }
    return jsonify(data)


# ============================================================
# Google account linking / sign-in
#
# Requires the explicit SQL migration:
#   migrations/20261009_google_accounts.sql
#
# Environment:
#   GOOGLE_CLIENT_ID
#   GOOGLE_CLIENT_SECRET
#   GOOGLE_REDIRECT_URI  (must exactly match the Google OAuth client)
# ============================================================

from urllib.request import Request as _GoogleRequest, urlopen as _google_urlopen
from urllib.error import URLError as _GoogleURLError, HTTPError as _GoogleHTTPError
from urllib.parse import urlencode as _google_urlencode
from sqlalchemy import text as _google_sql_text

_GOOGLE_TICKET_TTL = 300
_GOOGLE_ALLOWED_MODES = {"link", "relink", "signin", "restore"}

def _google_config():
    return (
        os.environ.get("GOOGLE_CLIENT_ID", "").strip(),
        os.environ.get("GOOGLE_CLIENT_SECRET", "").strip(),
        os.environ.get("GOOGLE_REDIRECT_URI", "").strip(),
    )

def _google_json_body():
    payload = request.get_json(silent=True)
    return payload if isinstance(payload, dict) else {}

def _google_ticket_hash(ticket):
    return hashlib.sha256(ticket.encode("utf-8")).hexdigest()

def _google_db_ready():
    try:
        with db.engine.connect() as connection:
            connection.execute(_google_sql_text("SELECT 1 FROM google_account_links LIMIT 1"))
            connection.execute(_google_sql_text("SELECT 1 FROM google_account_tickets LIMIT 1"))
        return True
    except Exception:
        app.logger.exception("Google account tables are missing; apply migrations/20261009_google_accounts.sql")
        return False

def _google_player_header():
    username = (request.headers.get("Player-Id") or "").strip()
    if not username or len(username) > 80:
        return None
    return username

def _google_ticket_row(connection, ticket):
    return connection.execute(
        _google_sql_text("""
            SELECT ticket_hash, mode, player_username, oauth_state, status,
                   result_username, email, google_sub, error, can_create,
                   expires_at, consumed
            FROM google_account_tickets
            WHERE ticket_hash = :ticket_hash
        """),
        {"ticket_hash": _google_ticket_hash(ticket)}
    ).mappings().first()

def _google_set_ticket_error(ticket_hash, message):
    with db.engine.begin() as connection:
        connection.execute(
            _google_sql_text("""
                UPDATE google_account_tickets
                SET status = 'error', error = :error, can_create = 0
                WHERE ticket_hash = :ticket_hash AND consumed = 0
            """),
            {"error": message[:240], "ticket_hash": ticket_hash}
        )

def _google_exchange_code(code):
    client_id, client_secret, redirect_uri = _google_config()
    if not client_id or not client_secret or not redirect_uri:
        raise RuntimeError("Google OAuth server configuration is incomplete.")
    form = _google_urlencode({
        "code": code,
        "client_id": client_id,
        "client_secret": client_secret,
        "redirect_uri": redirect_uri,
        "grant_type": "authorization_code",
    }).encode("utf-8")
    token_request = _GoogleRequest(
        "https://oauth2.googleapis.com/token",
        data=form,
        headers={"Content-Type": "application/x-www-form-urlencoded"},
        method="POST",
    )
    with _google_urlopen(token_request, timeout=12) as response:
        token_payload = json.loads(response.read().decode("utf-8"))
    id_token = token_payload.get("id_token")
    if not id_token:
        raise RuntimeError("Google did not return an ID token.")

    # Google validates the signature and token claims on this endpoint.
    verify_url = "https://oauth2.googleapis.com/tokeninfo?" + _google_urlencode({"id_token": id_token})
    verify_request = _GoogleRequest(verify_url, headers={"Accept": "application/json"})
    with _google_urlopen(verify_request, timeout=12) as response:
        identity = json.loads(response.read().decode("utf-8"))

    if identity.get("aud") != client_id:
        raise RuntimeError("Google token audience did not match this game.")
    if identity.get("iss") not in ("accounts.google.com", "https://accounts.google.com"):
        raise RuntimeError("Google token issuer was invalid.")
    if not identity.get("sub"):
        raise RuntimeError("Google identity did not include a stable subject.")
    if str(identity.get("email_verified", "")).lower() != "true":
        raise RuntimeError("The Google email address is not verified.")
    return {
        "sub": str(identity["sub"]),
        "email": str(identity.get("email", ""))[:320],
    }

def _google_finish_identity(ticket_hash, google_sub, email):
    now = int(time.time())
    with db.engine.begin() as connection:
        row = connection.execute(
            _google_sql_text("""
                SELECT mode, player_username, expires_at, consumed
                FROM google_account_tickets
                WHERE ticket_hash = :ticket_hash
            """),
            {"ticket_hash": ticket_hash}
        ).mappings().first()
        if not row or row["consumed"] or int(row["expires_at"]) < now:
            return False, "This Google sign-in request expired. Please try again."

        mode = row["mode"]
        player_username = row["player_username"]

        linked = connection.execute(
            _google_sql_text("""
                SELECT player_username FROM google_account_links
                WHERE google_sub = :google_sub
            """),
            {"google_sub": google_sub}
        ).mappings().first()

        if mode in ("link", "relink"):
            if not player_username:
                return False, "The game account was not identified."
            if linked and linked["player_username"] != player_username:
                return False, "This Google account is already linked to another game account."
            if not connection.execute(
                _google_sql_text("SELECT username FROM player WHERE username = :username"),
                {"username": player_username}
            ).first():
                return False, "The game account no longer exists."

            if mode == "relink":
                connection.execute(
                    _google_sql_text("DELETE FROM google_account_links WHERE player_username = :username"),
                    {"username": player_username}
                )
            connection.execute(
                _google_sql_text("""
                    INSERT INTO google_account_links (google_sub, player_username, email, linked_at)
                    VALUES (:google_sub, :username, :email, :linked_at)
                    ON CONFLICT(google_sub) DO UPDATE SET
                        player_username = excluded.player_username,
                        email = excluded.email,
                        linked_at = excluded.linked_at
                """),
                {"google_sub": google_sub, "username": player_username, "email": email, "linked_at": now}
            )
            connection.execute(
                _google_sql_text("""
                    UPDATE google_account_tickets
                    SET status = 'ok', result_username = :username, email = :email,
                        google_sub = :google_sub, error = NULL, can_create = 0
                    WHERE ticket_hash = :ticket_hash
                """),
                {"username": player_username, "email": email, "google_sub": google_sub, "ticket_hash": ticket_hash}
            )
            return True, None

        if linked:
            username = linked["player_username"]
            connection.execute(
                _google_sql_text("""
                    UPDATE google_account_tickets
                    SET status = 'ok', result_username = :username, email = :email,
                        google_sub = :google_sub, error = NULL, can_create = 0
                    WHERE ticket_hash = :ticket_hash
                """),
                {"username": username, "email": email, "google_sub": google_sub, "ticket_hash": ticket_hash}
            )
            return True, None

        connection.execute(
            _google_sql_text("""
                UPDATE google_account_tickets
                SET status = 'needs_create', email = :email, google_sub = :google_sub,
                    error = 'No game account is linked to this Google account.',
                    can_create = 1
                WHERE ticket_hash = :ticket_hash
            """),
            {"email": email, "google_sub": google_sub, "ticket_hash": ticket_hash}
        )
        return True, None

def _google_error_page(message, status=400):
    safe = (message or "Google sign-in could not be completed.")
    safe = safe.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;").replace('"', "&quot;")
    return make_response(
        "<!doctype html><html><head><meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>Card Wars Kingdom</title></head><body style='font-family:system-ui;padding:2rem'>"
        "<h2>Card Wars Kingdom</h2><p>" + safe +
        "</p><p>You can return to the game.</p></body></html>",
        status,
        {"Content-Type": "text/html; charset=utf-8"},
    )

@app.route("/account/google/begin", methods=["POST"])
def GoogleAccountBegin():
    if not _google_db_ready():
        return jsonify({"error": "Google account service is not configured. Apply the database migration first."}), 503
    body = _google_json_body()
    mode = str(body.get("mode", "")).lower().strip()
    if mode not in _GOOGLE_ALLOWED_MODES:
        return jsonify({"error": "Unsupported Google account operation."}), 400
    if mode in ("link", "relink"):
        username = _google_player_header()
        if not username:
            return jsonify({"error": "Player-Id header is required."}), 401
        if Player.query.filter_by(username=username).first() is None:
            return jsonify({"error": "Game account was not found."}), 404
        if IsUserBanned(username, IPFromRequest(request)):
            return jsonify({"error": "This game account is banned."}), 403
    else:
        username = None

    client_id, client_secret, redirect_uri = _google_config()
    if not client_id or not client_secret or not redirect_uri:
        return jsonify({"error": "Google OAuth server configuration is incomplete."}), 503

    ticket = secrets.token_urlsafe(32)
    ticket_hash = _google_ticket_hash(ticket)
    now = int(time.time())
    with db.engine.begin() as connection:
        connection.execute(
            _google_sql_text("""
                INSERT INTO google_account_tickets
                    (ticket_hash, mode, player_username, oauth_state, status,
                     created_at, expires_at, consumed, can_create)
                VALUES (:ticket_hash, :mode, :username, :oauth_state, 'pending',
                        :created_at, :expires_at, 0, 0)
            """),
            {
                "ticket_hash": ticket_hash,
                "mode": mode,
                "username": username,
                "oauth_state": secrets.token_urlsafe(32),
                "created_at": now,
                "expires_at": now + _GOOGLE_TICKET_TTL,
            }
        )
        # Return the state only through the browser URL generation, never to the Unity client.
        row = connection.execute(
            _google_sql_text("SELECT oauth_state FROM google_account_tickets WHERE ticket_hash = :ticket_hash"),
            {"ticket_hash": ticket_hash}
        ).first()
        oauth_state = row[0]

    return jsonify({"ticket": ticket, "status": "pending"})

@app.route("/account/google/start", methods=["GET"])
def GoogleAccountStart():
    ticket = (request.args.get("ticket") or "").strip()
    if not ticket or not _google_db_ready():
        return _google_error_page("This Google sign-in request is invalid or expired.")
    now = int(time.time())
    with db.engine.connect() as connection:
        row = _google_ticket_row(connection, ticket)
    if not row or row["consumed"] or int(row["expires_at"]) < now or row["status"] != "pending":
        return _google_error_page("This Google sign-in request expired. Return to the game and try again.")
    client_id, _, redirect_uri = _google_config()
    if not client_id or not redirect_uri:
        return _google_error_page("Google OAuth server configuration is incomplete.", 503)
    auth_url = "https://accounts.google.com/o/oauth2/v2/auth?" + _google_urlencode({
        "client_id": client_id,
        "redirect_uri": redirect_uri,
        "response_type": "code",
        "scope": "openid email",
        "state": row["oauth_state"],
        "prompt": "select_account",
    })
    return redirect(auth_url, code=302)

@app.route("/account/google/callback", methods=["GET"])
def GoogleAccountCallback():
    if request.args.get("error"):
        return _google_error_page("Google sign-in was cancelled or denied.")
    code = (request.args.get("code") or "").strip()
    state = (request.args.get("state") or "").strip()
    if not code or not state or not _google_db_ready():
        return _google_error_page("Google returned an invalid sign-in response.")
    with db.engine.connect() as connection:
        row = connection.execute(
            _google_sql_text("""
                SELECT ticket_hash, expires_at, consumed
                FROM google_account_tickets
                WHERE oauth_state = :state
            """),
            {"state": state}
        ).mappings().first()
    if not row or row["consumed"] or int(row["expires_at"]) < int(time.time()):
        return _google_error_page("This Google sign-in request expired. Return to the game and try again.")
    try:
        identity = _google_exchange_code(code)
        ok, error = _google_finish_identity(row["ticket_hash"], identity["sub"], identity["email"])
        if not ok:
            _google_set_ticket_error(row["ticket_hash"], error or "Google sign-in failed.")
            return _google_error_page(error or "Google sign-in failed.")
    except (_GoogleHTTPError, _GoogleURLError, TimeoutError, ValueError, RuntimeError) as exc:
        app.logger.warning("Google OAuth callback failed: %s", type(exc).__name__)
        _google_set_ticket_error(row["ticket_hash"], "Google verification failed. Please try again.")
        return _google_error_page("Google verification failed. Return to the game and try again.")
    return _google_error_page("Google verification completed. Return to Card Wars Kingdom.", 200)

@app.route("/account/google/poll", methods=["GET"])
def GoogleAccountPoll():
    ticket = (request.args.get("ticket") or "").strip()
    if not ticket or not _google_db_ready():
        return jsonify({"status": "error", "error": "Invalid or expired request.", "can_create": False}), 400
    now = int(time.time())
    with db.engine.begin() as connection:
        row = _google_ticket_row(connection, ticket)
        if not row:
            return jsonify({"status": "error", "error": "Invalid or expired request.", "can_create": False}), 404
        if int(row["expires_at"]) < now and not row["consumed"]:
            connection.execute(
                _google_sql_text("""
                    UPDATE google_account_tickets
                    SET status = 'error', error = 'This request expired.', can_create = 0
                    WHERE ticket_hash = :ticket_hash AND status = 'pending'
                """),
                {"ticket_hash": row["ticket_hash"]}
            )
            row = _google_ticket_row(connection, ticket)
    if row["status"] == "ok":
        return jsonify({
            "status": "ok",
            "username": row["result_username"],
            "email": row["email"] or "",
            "already": False,
        })
    if row["status"] == "needs_create":
        return jsonify({
            "status": "error",
            "error": row["error"] or "No game account is linked to this Google account.",
            "can_create": True,
        })
    if row["status"] == "error":
        return jsonify({"status": "error", "error": row["error"] or "Google sign-in failed.", "can_create": False})
    return jsonify({"status": "pending"})

@app.route("/account/google/create", methods=["POST"])
def GoogleAccountCreate():
    if not _google_db_ready():
        return jsonify({"error": "Google account service is not configured."}), 503
    ticket = str(_google_json_body().get("ticket", "")).strip()
    if not ticket:
        return jsonify({"error": "A valid Google sign-in ticket is required."}), 400
    now = int(time.time())
    connection = db.session.connection()
        row = _google_ticket_row(connection, ticket)
        if not row or row["consumed"] or int(row["expires_at"]) < now:
            return jsonify({"error": "This Google sign-in request expired. Start again."}), 400
        if row["status"] != "needs_create" or not row["google_sub"]:
            return jsonify({"error": "Google identity has not been verified for account creation."}), 400
        existing_link = connection.execute(
            _google_sql_text("SELECT player_username FROM google_account_links WHERE google_sub = :sub"),
            {"sub": row["google_sub"]}
        ).first()
        if existing_link:
            connection.execute(
                _google_sql_text("""
                    UPDATE google_account_tickets SET status='ok', result_username=:username,
                        can_create=0, consumed=1 WHERE ticket_hash=:ticket_hash
                """),
                {"username": existing_link[0], "ticket_hash": row["ticket_hash"]}
            )
            return jsonify({"username": existing_link[0]})
        # Generate a non-guessable, valid legacy username. No user-supplied username is trusted.
        username = None
        for _ in range(8):
            candidate = "google_" + secrets.token_hex(8)
            if not InvalidUsername(candidate) and Player.query.filter_by(username=candidate).first() is None:
                username = candidate
                break
        if username is None:
            return jsonify({"error": "Could not allocate a game account. Please try again."}), 500
        player = Player(username=username)
        db.session.add(player)
        db.session.flush()
        connection.execute(
            _google_sql_text("""
                INSERT INTO google_account_links (google_sub, player_username, email, linked_at)
                VALUES (:sub, :username, :email, :linked_at)
            """),
            {"sub": row["google_sub"], "username": username, "email": row["email"] or "", "linked_at": now}
        )
        connection.execute(
            _google_sql_text("""
                UPDATE google_account_tickets SET status='ok', result_username=:username,
                    error=NULL, can_create=0, consumed=1
                WHERE ticket_hash=:ticket_hash
            """),
            {"username": username, "ticket_hash": row["ticket_hash"]}
        )
        db.session.commit()
    PlayerLog(IPFromRequest(request), username, "Created new player through verified Google sign-in")
    return jsonify({"username": username})

@app.route("/account/google/status", methods=["POST"])
def GoogleAccountStatus():
    if not _google_db_ready():
        return jsonify({"linked": False, "email": "", "reward": False, "relink": False, "error": "Google account service is not configured."}), 503
    username = _google_player_header()
    if not username:
        return jsonify({"linked": False, "email": "", "reward": False, "relink": False}), 401
    with db.engine.connect() as connection:
        row = connection.execute(
            _google_sql_text("""
                SELECT email FROM google_account_links WHERE player_username = :username
            """),
            {"username": username}
        ).first()
    return jsonify({
        "linked": row is not None,
        "email": (row[0] or "") if row else "",
        "reward": False,
        "relink": row is not None,
    })

@app.route("/account/google/unlink", methods=["POST"])
def GoogleAccountUnlink():
    if not _google_db_ready():
        return jsonify({"error": "Google account service is not configured."}), 503
    username = _google_player_header()
    if not username:
        return jsonify({"error": "Player-Id header is required."}), 401
    with db.engine.begin() as connection:
        result = connection.execute(
            _google_sql_text("DELETE FROM google_account_links WHERE player_username = :username"),
            {"username": username}
        )
    return jsonify({"ok": True, "unlinked": result.rowcount > 0})


@app.route("/account/preAuth/")
def AccountPreAuth():
	data = {
		"data": {
			"nonce": os.urandom(32).hex()
		}
	}
	return jsonify(data)

@app.route("/account/gcAuth/", methods=['POST'])
def AccountGCAuth():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}

	if InvalidUsername(clientData["player_id"]):
		return make_response("Invalid Username!", 400)

	if IsUserBanned(clientData["player_id"], IPFromRequest(request)):
		return make_response("User is banned!", 400)

	#Create user if it doesn't exist
	db_user = Player.query.filter_by(username=clientData["player_id"]).first()

	isplayernew = False

	if db_user is None:
		db_user = Player(username=clientData["player_id"])
		db.session.add(db_user)
		db.session.commit()
		isplayernew = True
		PlayerLog(ip=IPFromRequest(request), player=clientData["player_id"], message="Created new player")


	data = {
		"data": {
			"user_id": clientData["player_id"],
			"is_new": isplayernew
		}
	}
	return data

@app.route("/persist/getcc/")
def GetCountryCode():
	data = {
		"ip": request.headers.get("X-Forwarded-For", request.remote_addr),
		"country_code": "US"
	}
	return jsonify(data)

@app.route("/multiplayer/new_player/", methods=['POST'])
def MultiplayerNewPlayer():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}

	#Make sure username is valid
	if InvalidUsername(clientData["name"]):
		return make_response("Invalid username!", 400)

	db_user = Player.query.filter_by(username=clientData["player_id"]).first()
	if db_user is None:
		return make_response("No player found!", 404)

	db_user.multiplayer_name = clientData["name"]
	db_user.icon = clientData["icon"]
	db_user.deck_rank = clientData["deck_rank"]
	db_user.landscapes = clientData["landscapes"]
	db_user.helper_creature = clientData["helper_creature"]
	db_user.leader = clientData["leader"]
	db_user.leader_level = clientData["leader_level"]
	db_user.allyboxspace = clientData["allyboxspace"]
	db_user.level = clientData["level"]
	db.session.commit()

	PlayerLog(IPFromRequest(request), clientData["player_id"], "Updated player data")

	return jsonify({
		"success": True,
		"data": {
			"name": clientData["name"],
			"icon": clientData["icon"],
			"leader": clientData["leader"],
			"level": str(clientData["leader_level"]),
			"trophies": "0" #Unused
		}
	})

@app.route("/multiplayer/update_deck_name/", methods=['POST'])
def MultiplayerUpdateDeckName():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}

	if InvalidUsername(clientData["name"]):
		return make_response("Invalid username!", 400)

	db_user = Player.query.filter_by(username=clientData["player_id"]).first()
	if db_user is None:
		return make_response("No player found!", 404)

	db_user.deck_rank = clientData["deck_rank"]
	db_user.landscapes = clientData["landscapes"]
	db_user.helper_creature = clientData["helper_creature"]
	db_user.leader = clientData["leader"]



	db_user.leader_level = clientData["leader_level"]
	db_user.allyboxspace = clientData["allyboxspace"]

	db.session.commit()

	PlayerLog(IPFromRequest(request), clientData["player_id"], "Updated player data")

	return jsonify({
		"success": True
	})

def get_hash_string(source_value, key):
	hmac_sha256 = hmac.new(key.encode('utf-8'), source_value.encode('utf-8'), hashlib.sha256)
	return hmac_sha256.hexdigest()

@app.route("/persist/user_action2/", methods=['POST'])
def UserAction2():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}

	if IsUserBanned(clientData["player_id"], IPFromRequest(request)):
		return make_response("User is banned!", 400)

	UpdateLastOnline(clientData["player_id"])

	#Check if an event was sent
	if "evt" in clientData:
		db_user = Player.query.filter_by(username=clientData["player_id"]).first()
		if db_user is None:
			return make_response("No player found!", 404)

		FreeHardCurrency = int(clientData["fr"])
		df = int(clientData["df"])

		finalamount = FreeHardCurrency + df

		PlayerLog(IPFromRequest(request), clientData["player_id"], "Updated player data")

		key = "5424498w34tiowhtgoae0tu4iksdf4_4" + clientData["player_id"] + "650"
		handle = get_hash_string(clientData["player_id"], key)

		data = {
			"success": True,
			"data": "{\"fields\": {\"level2\": " + str(finalamount) +  ", \"handle\": \"" + handle + "\"}}",
		}
	else:
		data = {
			"success": True,
		}

	return jsonify(data)

def InvalidUsername(username):
	username = username.lower()
	for char in badcharaters:
		if char in username:
			return True
	if username == 'ua' or username == 'guest':
		return True
	return False

def IsBanActive(ban):
	"""Return True for permanent bans or temporary bans that have not expired."""
	if ban is None:
		return False

	# expires_at=None means permanent ban.
	if ban.expires_at is None:
		return True

	return int(time.time()) < int(ban.expires_at)


def GetActiveBan(identifier):
	"""Return an active ban, automatically removing it if it has expired."""
	ban = Bans.query.filter_by(username=identifier).first()

	if ban is None:
		return None

	if IsBanActive(ban):
		return ban

	# Temporary ban expired. Remove it automatically.
	db.session.delete(ban)
	db.session.commit()
	return None


def IsUserBanned(username, ip=None):
	# Check user ban.
	if GetActiveBan(username) is not None:
		return True

	if ip is None:
		return False

	# Check IP ban.
	if GetActiveBan(ip) is not None:
		return True

	return False

@app.route("/persist/<string:id>/game", methods=['GET', 'PUT'])
def PersistGame(id):

	if request.headers.get("Player-Id") is None:
		DiscordWebhookMessage("User attempted to access game without Player-Id header. IP: " + IPFromRequest(request))
		abort(404)
	username = request.headers.get("Player-Id")

	#verify headers
	if request.headers.get("Age") is None:
		DiscordWebhookMessage(username +" attempted to access game without Age header. IP: " + IPFromRequest(request))
		abort(404)
	if request.headers.get("User-Agent") != "Innertube Explorer v0.1":
		DiscordWebhookMessage(username +" attempted to access game without User-Agent header. IP: " + IPFromRequest(request))
		abort(404)
	if request.headers.get("Platform") is None:
		DiscordWebhookMessage(username +" attempted to access game without Platform header. IP: " + IPFromRequest(request))
		abort(404)
	if request.headers.get("Version") is None:
		DiscordWebhookMessage(username +" attempted to access game without Version header. IP: " + IPFromRequest(request))
		abort(404)
	if request.method == 'PUT':
		if request.headers.get("X-Nick-Description") is None:
			DiscordWebhookMessage(username +" attempted to access game without X-Nick-Description header. IP: " + IPFromRequest(request))
			abort(404)

	if InvalidUsername(username):
		return make_response("Invalid Username!", 400)
	if IsUserBanned(username, IPFromRequest(request)):
		return make_response("No game found!", 404)

	#Device name check
	if request.method == 'PUT':
		DeviceNameUser = Player.query.filter_by(username=username).first()
		devicename = request.headers["X-Nick-Description"]

		#check if player's devicename is empty, if so, set it to X-Nick-Description
		if DeviceNameUser.devicename is None or DeviceNameUser.devicename == b"":
			DeviceNameUser.devicename = devicename
			db.session.commit()

		#check if player's devicename is the same as X-Nick-Description, if not, return error
		if DeviceNameUser.devicename != devicename:
			DiscordWebhookMessage(username + " attempted to access game with wrong device name. Device name: '" + devicename + "'. IP: " + IPFromRequest(request))
			return make_response("Invalid Username!", 400)

	UpdateLastOnline(username)

	#check if user is PVP banned
	pvp_ban_db_user = Player.query.filter_by(username=username).first()
	if pvp_ban_db_user is not None:
		try:
			game = DecryptGameData(pvp_ban_db_user.game)
			if game is not None:
				if int(game["Zxcvbnm"]) == 1:
					DiscordWebhookMessage(username +" attempted to access game while PVP banned. IP: " + IPFromRequest(request))
					SystemBan(username)
					return make_response("User is banned!", 400)
		except Exception as e:
			Log("persist", "Error while checking if user is PVP banned: " + str(e))

	if request.method == 'GET':
		db_user = Player.query.filter_by(username=username).first()
		if db_user is None:
			return make_response("No game found!", 404)
		if db_user.game is None:
			return make_response("No game found!", 404)
		if db_user.game == b"" or db_user.game == b" ":
			return make_response("No game found!", 404)
		return db_user.game

	if request.method == 'PUT':
		data = request.data

		#check if data is encrypted
		if not data.startswith(b"username=") or data.startswith(b"{"):
			DiscordWebhookMessage(username +" attempted to put game data without encryption. IP: " + IPFromRequest(request) + ". Data: " + data.decode("utf-8")[:50])

		db_user = Player.query.filter_by(username=username).first()
		if db_user is None:
			return make_response("No game found!", 404)
		db_user.game = data
		db.session.commit()
		return make_response("OK", 200)


def UpdateLastOnline(player_id):
	user = Player.query.filter_by(username=player_id).first()
	if user is None:
		return None
	user.last_online = int(time.time())
	db.session.commit()

def AllyBoxSpaceNotExceeded(player_id):
	user = Player.query.filter_by(username=player_id).first()
	if user is None:
		return None

	#count number of friends
	friends = json.loads(user.friends)

	friends_count = 0

	for friend in friends:
		#check if user is banned
		if IsUserBanned(friend):
			continue
		friend_user = Player.query.filter_by(username=friend).first()
		if friend_user is None:
			continue

		friends_count += 1

	return friends_count < user.allyboxspace

@app.route("/persist/friends/<string:player_id>")
def PersistFriends(player_id):
	UpdateLastOnline(player_id)
	db_user = Player.query.filter_by(username=player_id).first()

	if db_user is None:
		return make_response("No player found!", 404)

	data = []

	player_friends = json.loads(db_user.friends)

	for friend in player_friends:
		#check if user is banned
		if IsUserBanned(friend):
			continue
		friend_user = Player.query.filter_by(username=friend).first()
		allyinfo = GetAllyInfo(friend, True)
		if allyinfo is not None:
			data.append(allyinfo)

	return jsonify(data)

@app.route("/persist/friends_find_candidatesDW/", methods=['POST'])
def PersistFriendsFindCandidates():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}

	db_user = Player.query.filter_by(username=clientData["player_id"]).first()

	if db_user is None:
		return make_response("No player found!", 404)

	data = []

	player_friends = json.loads(db_user.friends)

	for friend in player_friends:
		#check if user is banned
		if IsUserBanned(friend):
			continue
		friend_user = Player.query.filter_by(username=friend).first()
		#add ally if the level is withen clientData["level"]
		allyinfo = GetAllyInfo(friend, True)
		if allyinfo is not None:
			data.append(allyinfo)

	#Add explorers
	strangers = Player.query.filter(
		Player.username != clientData["player_id"],  # not the player
		Player.username.notin_(player_friends),  # not a friend
		Player.helper_creature != None,  # has a helper creature
		Player.leader_level.between(db_user.leader_level - int(clientData["level"]), db_user.leader_level + int(clientData["level"]))  # level is within clientData["level"]
	).order_by(func.random()).limit(3).all()

	for stranger in strangers:
		if IsUserBanned(stranger.username):
			continue

		allyinfo = GetAllyInfo(stranger.username, False)
		if allyinfo is not None:
			data.append(allyinfo)

	#randomize list
	data = random.sample(data, len(data))

	data2 = {
		"success": True,
		"data": json.dumps(data)
	}
	return jsonify(data2)

@app.route("/persist/friends_use_friendDW/", methods=['POST'])
def PersistFriendsUseFriend():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}


	db_ally = Player.query.filter_by(username=clientData["friendid"]).first()

	if db_ally is None:
		return make_response("No player found!", 500)

	db_ally.helpcount = int(db_ally.helpcount) + 1
	db.session.commit()

	data = {
		"success": True,
	}
	return jsonify(data)

@app.route("/persist/friends_use_playerDW/", methods=['POST'])
def PersistFriendsUsePlayer():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}


	db_stranger = Player.query.filter_by(username=clientData["userid"]).first()
	if db_stranger is None:
		return make_response("No player found!", 404)

	db_stranger.anonymoushelpcount = int(db_stranger.anonymoushelpcount) + 1
	db.session.commit()

	data = {
		"success": True,
	}
	return jsonify(data)

@app.route("/persist/friends_request_withmyinfoDW/", methods=['POST'])
def PersistFriendsRequestWithMyInfo():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}

	UpdateLastOnline(clientData["player_id"])

	try:
		invite_user = Player.query.filter_by(username=clientData["invite_id"].replace("_", "-")).first()
	except:
		return make_response("No player found!", 400)
	if invite_user is None:
		return make_response("No player found!", 400)

	db_user = Player.query.filter_by(username=clientData["player_id"]).first()
	if db_user is None:
		return make_response("No player found!", 400)


	inviteuserfr = json.loads(invite_user.friend_requests)

	#Player ally check
	allycheck = AllyBoxSpaceNotExceeded(clientData["player_id"])
	if allycheck == False:
		return jsonify({
			"success": True,
			"info": "exceed me"
		})

	#friend ally check
	friendallycheck = AllyBoxSpaceNotExceeded(clientData["invite_id"].replace("_", "-"))
	if friendallycheck == False:
		return jsonify({
			"success": True,
			"info": "exceed"
		})

	#check if player already sent a request
	if clientData["player_id"] not in invite_user.friend_requests:
		inviteuserfr.append(clientData["player_id"])
		invite_user.friend_requests = json.dumps(inviteuserfr)
		db.session.commit()
		return jsonify({
			"success": True
		})
	else:
		return jsonify({
			"success": True,
			"info": "duplicate"
		})

def GetAllyInfo(player_id: str, isally: bool):
	db_user = Player.query.filter_by(username=player_id).first()
	if db_user is None:
		return None
	if db_user.multiplayer_name is None:
		return None
	data = {
		"fields": {
			"user_id": db_user.username,
			"name": db_user.multiplayer_name,
			"icon": db_user.icon,
			"rankxp": db_user.leader_level,
			"helpcount": db_user.helpcount if db_user.helpcount is not None else "0",
			"anonymoushelpcount": db_user.anonymoushelpcount if db_user.anonymoushelpcount is not None else "0",
			"helpercreatureid": db_user.leader,
			"helpercreature": db_user.helper_creature,
			"landscapes": db_user.landscapes,
			"ally": "1" if isally else "0",
			"sincelastactivedate": str(int(time.time()) - db_user.last_online)
		}
	}
	return data

@app.route("/persist/friends_all_requests_received/<string:player_id>", methods=['GET'])
def PersistFriendsAllRequestsReceived(player_id):
	db_user = Player.query.filter_by(username=player_id).first()
	if db_user is None:
		return make_response("No player found!", 400)

	data = []

	playerfriendrequests = json.loads(db_user.friend_requests)

	for friendrequest in playerfriendrequests:
		allyinfo = GetAllyInfo(friendrequest, False)
		if allyinfo is not None:
			data.append(allyinfo)

	return jsonify(data)

@app.route("/persist/friends_deny_request/<string:player_id>/<string:invite_id>", methods=['GET'])
def PersistFriendsDenyRequest(player_id, invite_id):
	db_user = Player.query.filter_by(username=player_id).first()
	if db_user is None:
		return make_response("No player found!", 400)

	UpdateLastOnline(player_id)

	#remove friend request
	player_requests = json.loads(db_user.friend_requests)
	player_requests.remove(invite_id)
	db_user.friend_requests = json.dumps(player_requests)

	db.session.commit()
	return jsonify({
		"success": True
	})

@app.route("/persist/friends_confirm_request_withmyinfoDW/", methods=['POST'])
def PersistFriendsConfirmRequestWithMyInfo():
	clientData = parse_qs(request.get_data().decode('utf-8'))
	clientData = {k: v[0] if len(v) == 1 else v for k, v in clientData.items()}

	UpdateLastOnline(clientData["player_id"])

	db_user = Player.query.filter_by(username=clientData["player_id"]).first()
	if db_user is None:
		return make_response("No player found!", 400)

	#Player ally check
	allycheck = AllyBoxSpaceNotExceeded(clientData["player_id"])
	if allycheck == False:
		return jsonify({
			"success": True,
			"info": "exceed me"
		})

	#friend ally check
	friendallycheck = AllyBoxSpaceNotExceeded(clientData["invite_id"])
	if friendallycheck == False:
		return jsonify({
			"success": True,
			"info": "exceed"
		})

	#remove friend request
	player_requests = json.loads(db_user.friend_requests)
	player_requests.remove(clientData["invite_id"])
	db_user.friend_requests = json.dumps(player_requests)

	#add friend
	player_friends = json.loads(db_user.friends)
	player_friends.append(clientData["invite_id"])
	db_user.friends = json.dumps(player_friends)

	#add self to friend's friend list
	friend_user = Player.query.filter_by(username=clientData["invite_id"]).first()

	if friend_user is None:
		return make_response("No player found!", 400)

	friend_friends = json.loads(friend_user.friends)
	friend_friends.append(clientData["player_id"])
	friend_user.friends = json.dumps(friend_friends)

	db.session.commit()

	return jsonify({
		"success": True
	})

@app.route("/persist/friends_remove/<string:player_id>/<string:invite_id>", methods=['GET'])
def PersistFriendsRemove(player_id, invite_id):
	db_user = Player.query.filter_by(username=player_id).first()
	if db_user is None:
		return make_response("No player found!", 400)

	#remove friend
	player_friends = json.loads(db_user.friends)
	player_friends.remove(invite_id)
	db_user.friends = json.dumps(player_friends)

	#remove self from friend
	friend_user = Player.query.filter_by(username=invite_id).first()

	if friend_user is None:
		return make_response("No player found!", 400)

	friend_friends = json.loads(friend_user.friends)
	friend_friends.remove(player_id)
	friend_user.friends = json.dumps(friend_friends)

	db.session.commit()

	return jsonify({
		"success": True
	})

@app.route("/analytics/upsight", methods=['POST'])
def AnalyticsUpsight():
	headers = request.headers

	if headers.get("Player-Id") is None or headers.get("Event-Type") is None or headers.get("Event-Action") is None:
		return make_response("Bad request!", 400)

	message = request.get_data().decode('utf-8')
	if message == "null":
		message = None

	newAnalytics = UpsightLogs(
		player_id=headers.get("Player-Id"),
		time=int(time.time()),
		event=headers.get("Event-Type"),
		action=headers.get("Event-Action"),
		message=message
	)
	db.session.add(newAnalytics)
	db.session.commit()

	if headers.get("Event-Action") == "detector":
		DiscordWebhookMessage(headers.get("Player-Id") + " triggered ACTk Anti-Cheat. Data:" + message)
		SystemBan(headers.get("Player-Id"))

	return make_response("OK", 200)

@app.route("/analytics/pvpmatch", methods=['POST'])
def AnalyticsPVPMatch():
	headers = request.headers

	if headers.get("Player-Id") is None:
		return make_response("Bad request!", 400)

	message = request.get_data().decode('utf-8')
	if message == "null":
		message = None

	#write to file
	os.makedirs("data/persist/pvpmatches", exist_ok=True)

	with open("data/persist/pvpmatches/" + headers.get("Player-Id", "unknown") +"_"+ headers.get("Match-Id", "unknown") + ".json", "w") as outfile:
		message = json.loads(message)
		json.dump(message, outfile, indent=4)

	return make_response("OK", 200)

@app.route("/dw_leaderboard/fetchentries/", methods=["POST"])
def FetchLeaderboardsEntriesEndpoint():
    try:
        payload = request.get_json(silent=True) or request.form

        start_pos = int(payload.get("startpos", 1))
        end_pos = int(payload.get("endpos", 30))

        pvp_ranks_file_path = "data/persist/blueprints/db_PVPRanks.json"

        leaderboard_list = []

        # Admin panel ile aynı veri kaynağı
        for player in Player.query.all():

            try:
                game = DecryptGameData(player.game)

                if game is None:
                    continue

                multiplayer_level = int(
                    game.get("MultiplayerLevel", 999)
                )

                points = int(
                    game.get("PointsInMultiplayerLevel", 0)
                )

                try:
                    rank_name, sprite_name, rank_id = get_rank_details_from_json(
                        points,
                        pvp_ranks_file_path
                    )
                except Exception:
                    rank_name = "Unknown"
                    sprite_name = ""
                    rank_id = 0

                leaderboard_list.append({
                    "playerid": player.username,
                    "playername": game.get(
                        "MultiplayerPlayerName",
                        "Unknown"
                    ),
                    "multiplayer_level": multiplayer_level,
                    "score": points,
                    "rank_name": rank_name,
                    "sprite_name": sprite_name,
                    "rank_id": rank_id
                })

            except Exception:
                continue

        # Admin panel ile aynı sıralama
        leaderboard_list.sort(
            key=lambda x: (
                x["multiplayer_level"] == -1,
                x["multiplayer_level"],
                -x["score"]
            )
        )

        # Ranking numarası ver
        for index, item in enumerate(leaderboard_list, start=1):
            item["ranking"] = index

        # En az 30 oyuncu
        minimum = max(30, end_pos)

        while len(leaderboard_list) < minimum:

            bot_index = len(leaderboard_list) + 1

            fake_points = max(
                100,
                22000 - bot_index * 650
            )

            try:
                rank_name, sprite_name, rank_id = get_rank_details_from_json(
                    fake_points,
                    pvp_ranks_file_path
                )
            except Exception:
                rank_name = "Unknown"
                sprite_name = ""
                rank_id = 0

            leaderboard_list.append({
                "ranking": bot_index,
                "playerid": f"bot_{bot_index}",
                "playername": f"Bot_{bot_index}",
                "multiplayer_level": 999,
                "score": fake_points,
                "rank_name": rank_name,
                "sprite_name": sprite_name,
                "rank_id": rank_id
            })

        return jsonify({
            "success": True,
            "data": leaderboard_list[start_pos - 1:end_pos]
        })

    except Exception as e:
        return jsonify({
            "success": False,
            "error": str(e)
        }), 500

@app.route("/dw_leaderboard/placeme/", methods=['POST'])
def place_me_on_leaderboard():
    try:
        req_data = request.get_json(force=True) if request.is_json else request.form
        # Oyuncuyu liderlik tablosuna kaydetme mantığı buraya gelir

        return jsonify({"success": True}), 200
    except Exception as e:
        return jsonify({"success": False, "error": str(e)}), 500


@app.route("/dw_leaderboard/registerresult/", methods=['POST'])
def register_match_result():
    try:
        req_data = request.get_json(force=True) if request.is_json else request.form
        # Maç sonucuna göre oyuncunun yeni puanını hesaplama mantığı buraya gelir

        # Örnek olarak oyuncunun yeni puanının 1500 olduğunu varsayalım
        # Unity callback((int)data["data"], ResponseFlag.Success) bekliyor
        return jsonify({"success": True, "data": 1500}), 200
    except Exception as e:
        return jsonify({"success": False, "data": 0}), 500


@app.route("/dw_leaderboard/hasended/", methods=['POST'])
def has_season_ended():
    try:
        # Sezon bitti mi kontrolü (0: Bitmedi, 1: Bitti)
        return jsonify({"success": True, "data": 0}), 200
    except Exception as e:
        return jsonify({"success": False, "data": 0}), 500

def GetPlayerWins(player_id):
	db_user = Player.query.filter_by(username=player_id).first()
	if db_user is None:
		return None

	if db_user.game is None or db_user.game == b"" or db_user.game == b" ":
		return None

	#check if player is game banned
	if IsUserBanned(player_id):
		return None

	#decrypt game
	try:
		game = DecryptGameData(db_user.game)
	except Exception:
		return None

	#check if user is PVP banned
	if game["Zxcvbnm"]:
		return None

	#which season is it?
	currentSeason = ""
	with open('data/persist/blueprints/db_PVPSeasons.json') as f:
		seasons = json.load(f)
		seasons = list(filter(lambda x: "EndDate" in x, seasons))
		#find the current season
		for season in seasons:
			#convert enddate to unix time
			if int(time.time()) < datetime.strptime(season["EndDate"], "%m/%d/%Y").timestamp():
				currentSeason = season["Season"]
				break
		#if the time is after enddate, use the last season
		if currentSeason == "":
			currentSeason = seasons[-1]["Season"]

	if game["ActivePvpSeason"] != currentSeason:
		return None

	if int(game["PvpPlayed"]) == 0:
		return None

	#count up wins
	#for each youWon: true in each battle history, add 1
	wins = 0
	for battle in game["BattleHistory"]:
		if battle["youWon"] == True and battle["season"] == currentSeason:
			wins += 1
	return wins

def run_scheduler():
	schedule.every(40000).hours.do(lambda: Backup())
	while True:
		try:
			schedule.run_pending()
			time.sleep(1)
		except Exception as e:
			Log("admin", f"Scheduler crashed: {str(e)}")


def Log(category, message):
	os.makedirs("data/persist/logs", exist_ok=True)
	date = datetime.now().strftime("%Y-%m-%d")
	time = datetime.now().strftime("%H:%M:%S")
	with open("data/persist/logs/" + date + ".txt", "a") as f:
		log = f"{time} - [{category.upper()}] - {message} \n"
		f.write(log)

# Create database tables and run migrations
with app.app_context():
        db.create_all()
        MigrateAdminSecurity()
        MigrateBanExpiration()


if __name__ == '__main__':
    Log("server", "Starting server...")

    # create version.txt and android_version.txt if they don't exist
    if not os.path.exists("data/persist/version.txt"):
        with open("data/persist/version.txt", "w") as f:
            f.write("1.0.0")

    if not os.path.exists("data/persist/android_version.txt"):
        with open("data/persist/android_version.txt", "w") as f:
            f.write("1.0.0")

    # Start scheduler
    scheduler_thread = threading.Thread(
        target=run_scheduler,
        daemon=True
    )
    scheduler_thread.start()

    # Start Flask server LAST
    app.run(
        debug=args.debug,
        port=args.port
    )
