import os
import sys
import base64
import getpass
from cryptography.fernet import Fernet

def encrypt_file(file_path, key):
    """Encrypts a Java file using the provided key."""
    cipher = Fernet(key)

    with open(file_path, "rb") as file:
        file_data = file.read()

    encrypted_data = cipher.encrypt(file_data)

    encrypted_file_path = file_path + ".enc"
    with open(encrypted_file_path, "wb") as enc_file:
        enc_file.write(encrypted_data)

    print(f"✅ File '{file_path}' has been encrypted and saved as '{encrypted_file_path}'.")

def decrypt_file(encrypted_file_path, key):
    """Decrypts an encrypted Java file using the provided key."""
    cipher = Fernet(key)

    with open(encrypted_file_path, "rb") as enc_file:
        encrypted_data = enc_file.read()

    try:
        decrypted_data = cipher.decrypt(encrypted_data)
    except Exception:
        print("❌ Decryption failed! Ensure you used the correct key.")
        return

    # Remove the .enc extension from the file name
    decrypted_file_path = encrypted_file_path.replace(".enc", "")

    with open(decrypted_file_path, "wb") as file:
        file.write(decrypted_data)

    print(f"✅ File '{encrypted_file_path}' has been decrypted and saved as '{decrypted_file_path}'.")

def main():
    if len(sys.argv) < 3:
        print("Usage:")
        print("  Encrypt: python3 encrypt_decrypt_java.py encrypt <file_path>")
        print("  Decrypt: python3 encrypt_decrypt_java.py decrypt <encrypted_file_path>")
        sys.exit(1)

    mode = sys.argv[1].lower()
    file_path = sys.argv[2]

    if not os.path.exists(file_path):
        print("❌ Error: The specified file does not exist.")
        sys.exit(1)

    key_input = getpass.getpass("🔑 Enter the encryption/decryption key: ").strip()

    try:
        key = base64.urlsafe_b64encode(key_input.encode().ljust(32)[:32])  # Ensure 32 bytes
        Fernet(key)  # Validate key
    except Exception:
        print("❌ Invalid key. Ensure it is a Base64 encoded 32-byte key.")
        sys.exit(1)

    if mode == "encrypt":
        encrypt_file(file_path, key)
    elif mode == "decrypt":
        decrypt_file(file_path, key)
    else:
        print("❌ Invalid mode. Use 'encrypt' or 'decrypt'.")
        sys.exit(1)

if __name__ == "__main__":
    main()
