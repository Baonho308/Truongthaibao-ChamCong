import 'package:flutter/material.dart';
import 'api_service.dart';

void main() => runApp(const ChamCongApp());

class ChamCongApp extends StatelessWidget {
  const ChamCongApp({super.key});

  @override
  Widget build(BuildContext context) => MaterialApp(
        debugShowCheckedModeBanner: false,
        title: 'TruongThaiBao - Chấm công',
        theme: ThemeData(colorSchemeSeed: Colors.indigo, useMaterial3: true),
        home: const LoginPage(),
      );
}

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final username = TextEditingController();
  final password = TextEditingController();
  final otp = TextEditingController();
  bool waitingOtp = false;
  bool busy = false;
  String message = '';

  Future<void> runLogin() async {
    setState(() { busy = true; message = ''; });
    try {
      if (!waitingOtp) {
        message = await ApiService.login(username.text.trim(), password.text);
        waitingOtp = true;
      } else {
        final result = await ApiService.verifyOtp(username.text.trim(), otp.text.trim());
        if (!mounted) return;
        Navigator.of(context).pushReplacement(MaterialPageRoute(
          builder: (_) => DashboardPage(
            token: result['token'] as String,
            role: result['role'] as String,
            username: result['username'] as String,
          ),
        ));
      }
    } catch (e) {
      message = e.toString();
    } finally {
      if (mounted) setState(() { busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('Đăng nhập chấm công')),
        body: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Icon(Icons.access_time_filled, size: 72),
                  const SizedBox(height: 20),
                  TextField(controller: username, decoration: const InputDecoration(labelText: 'Tên đăng nhập', border: OutlineInputBorder())),
                  const SizedBox(height: 12),
                  if (!waitingOtp) TextField(controller: password, obscureText: true, decoration: const InputDecoration(labelText: 'Mật khẩu', border: OutlineInputBorder())),
                  if (waitingOtp) ...[
                    TextField(controller: otp, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'OTP gửi qua email', border: OutlineInputBorder())),
                    const SizedBox(height: 8),
                    const Text('Nhập OTP nhận được qua email để hoàn tất đăng nhập.'),
                  ],
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: busy ? null : runLogin,
                    child: Text(busy ? 'Đang xử lý...' : waitingOtp ? 'Xác nhận OTP' : 'Đăng nhập'),
                  ),
                  if (message.isNotEmpty) Padding(
                    padding: const EdgeInsets.only(top: 12),
                    child: Text(message, style: TextStyle(color: message.startsWith('Exception') ? Colors.red : Colors.green)),
                  ),
                  TextButton(
                    onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const RegisterPage())),
                    child: const Text('Chưa có tài khoản? Đăng ký'),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
}

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});
  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final username = TextEditingController();
  final email = TextEditingController();
  final password = TextEditingController();
  final otp = TextEditingController();
  bool sent = false;
  bool busy = false;
  String message = '';

  Future<void> submit() async {
    setState(() { busy = true; message = ''; });
    try {
      if (!sent) {
        message = await ApiService.register(username.text.trim(), email.text.trim(), password.text);
        sent = true;
      } else {
        message = await ApiService.verifyEmail(username.text.trim(), otp.text.trim());
      }
    } catch (e) {
      message = e.toString();
    } finally {
      if (mounted) setState(() { busy = false; });
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Đăng ký tài khoản')),
    body: ListView(padding: const EdgeInsets.all(24), children: [
      TextField(controller: username, decoration: const InputDecoration(labelText: 'Tên đăng nhập', border: OutlineInputBorder())),
      const SizedBox(height: 12),
      if (!sent) ...[
        TextField(controller: email, keyboardType: TextInputType.emailAddress, decoration: const InputDecoration(labelText: 'Email', border: OutlineInputBorder())),
        const SizedBox(height: 12),
        TextField(controller: password, obscureText: true, decoration: const InputDecoration(labelText: 'Mật khẩu (ít nhất 8 ký tự)', border: OutlineInputBorder())),
      ] else
        TextField(controller: otp, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'OTP xác minh email', border: OutlineInputBorder())),
      const SizedBox(height: 16),
      FilledButton(onPressed: busy ? null : submit, child: Text(busy ? 'Đang xử lý...' : sent ? 'Xác minh email' : 'Đăng ký')),
      if (message.isNotEmpty) Padding(padding: const EdgeInsets.only(top: 12), child: Text(message)),
    ]),
  );
}

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key, required this.token, required this.role, required this.username});
  final String token;
  final String role;
  final String username;
  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  List<dynamic> records = [];
  String message = '';
  bool busy = false;

  Future<void> load() async {
    setState(() { busy = true; message = ''; });
    try { records = await ApiService.getAttendance(widget.token); }
    catch (e) { message = e.toString(); }
    finally { if (mounted) setState(() { busy = false; }); }
  }

  Future<void> checkIn() async {
    try {
      await ApiService.checkIn(widget.token);
      await load();
      if (mounted) setState(() => message = 'Chấm công vào thành công.');
    } catch (e) { if (mounted) setState(() => message = e.toString()); }
  }

  @override
  void initState() { super.initState(); load(); }

  @override
  Widget build(BuildContext context) {
    final isAdmin = widget.role.toLowerCase() == 'admin';
    final isEmployee = widget.role.toLowerCase() == 'employee';
    final title = isAdmin ? 'Trang quản trị Admin' : isEmployee ? 'Dashboard nhân viên' : 'Trang khách hàng';
    return Scaffold(
      appBar: AppBar(title: Text(title), actions: [
        IconButton(tooltip: 'Tải lại', onPressed: load, icon: const Icon(Icons.refresh)),
        IconButton(tooltip: 'Đăng xuất', onPressed: () => Navigator.of(context).pushAndRemoveUntil(
          MaterialPageRoute(builder: (_) => const LoginPage()), (_) => false), icon: const Icon(Icons.logout)),
      ]),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Text('Xin chào ${widget.username} (${widget.role})', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 12),
          if (isEmployee || isAdmin) FilledButton.icon(onPressed: checkIn, icon: const Icon(Icons.login), label: const Text('Chấm công vào')),
          const SizedBox(height: 8),
          OutlinedButton.icon(onPressed: load, icon: const Icon(Icons.history), label: const Text('Xem lịch sử chấm công')),
          if (message.isNotEmpty) Padding(padding: const EdgeInsets.symmetric(vertical: 8), child: Text(message)),
          if (busy) const LinearProgressIndicator(),
          Expanded(child: ListView.builder(
            itemCount: records.length,
            itemBuilder: (_, i) {
              final r = records[i] as Map<String, dynamic>;
              return Card(child: ListTile(
                leading: const Icon(Icons.event_available),
                title: Text('Bản ghi #${r['id']} - ${r['username'] ?? widget.username}'),
                subtitle: Text('Vào: ${r['checkInUtc']}\nRa: ${r['checkOutUtc'] ?? 'Chưa chấm công ra'}'),
              ));
            },
          )),
          const Text('CRUD quản trị đầy đủ có thể kiểm thử qua Swagger: PUT/DELETE chỉ dành cho Admin.'),
        ]),
      ),
    );
  }
}
