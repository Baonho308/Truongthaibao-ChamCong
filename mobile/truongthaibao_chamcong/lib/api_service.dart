import 'dart:convert';
import 'package:http/http.dart' as http;

class ApiService {
  // Android Emulator thường dùng 10.0.2.2 để truy cập máy host.
  // Hãy đổi port theo URL được in ra khi chạy dotnet run.
  static const String baseUrl = 'http://10.0.2.2:5000';

  static Future<String> register(String username, String email, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/register'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'username': username, 'email': email, 'password': password}),
    );
    if (response.statusCode >= 200 && response.statusCode < 300) return response.body.replaceAll('"', '');
    throw Exception(response.body);
  }

  static Future<String> verifyEmail(String username, String otp) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/verify-email'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'username': username, 'otp': otp}),
    );
    if (response.statusCode >= 200 && response.statusCode < 300) return response.body.replaceAll('"', '');
    throw Exception(response.body);
  }

  static Future<String> login(String username, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'username': username, 'password': password}),
    );
    if (response.statusCode >= 200 && response.statusCode < 300) return response.body.replaceAll('"', '');
    throw Exception(response.body);
  }

  static Future<Map<String, dynamic>> verifyOtp(String username, String otp) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/verify-otp'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({'username': username, 'otp': otp}),
    );
    if (response.statusCode >= 200 && response.statusCode < 300) {
      return jsonDecode(response.body) as Map<String, dynamic>;
    }
    throw Exception(response.body);
  }

  static Future<List<dynamic>> getAttendance(String token) async {
    final response = await http.get(
      Uri.parse('$baseUrl/api/attendance'),
      headers: {'Authorization': 'Bearer $token'},
    );
    if (response.statusCode == 200) return jsonDecode(response.body) as List<dynamic>;
    throw Exception(response.body);
  }

  static Future<void> checkIn(String token) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/attendance/check-in'),
      headers: {'Authorization': 'Bearer $token'},
    );
    if (response.statusCode < 200 || response.statusCode >= 300) throw Exception(response.body);
  }
}
