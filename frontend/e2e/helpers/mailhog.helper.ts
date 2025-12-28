interface MailHogAddress {
  Mailbox: string;
  Domain: string;
}

interface MailHogMessage {
  ID: string;
  From: MailHogAddress;
  To: MailHogAddress[];
  Content: {
    Headers: { Subject: string[] };
    Body: string;
  };
  Created: string;
}

interface MailHogResponse {
  total: number;
  count: number;
  start: number;
  items: MailHogMessage[];
}

export class MailHogHelper {
  private readonly baseUrl: string;

  constructor(baseUrl: string = 'http://localhost:8025') {
    this.baseUrl = baseUrl;
  }

  async getMessages(): Promise<MailHogMessage[]> {
    const response = await fetch(`${this.baseUrl}/api/v2/messages`);
    const data: MailHogResponse = await response.json();
    return data.items;
  }

  async getMessagesForEmail(email: string): Promise<MailHogMessage[]> {
    const messages = await this.getMessages();
    const [mailbox, domain] = email.split('@');
    return messages.filter((msg) =>
      msg.To.some(
        (to) =>
          to.Mailbox.toLowerCase() === mailbox.toLowerCase() &&
          to.Domain.toLowerCase() === domain.toLowerCase()
      )
    );
  }

  private decodeQuotedPrintable(str: string): string {
    // Decode quoted-printable encoding (e.g., =3D becomes =, =20 becomes space)
    return str
      .replace(/=\r?\n/g, '') // Remove soft line breaks
      .replace(/=([0-9A-Fa-f]{2})/g, (_, hex) => String.fromCharCode(parseInt(hex, 16)));
  }

  extractVerificationToken(message: MailHogMessage): string | null {
    const body = this.decodeQuotedPrintable(message.Content.Body);
    // Match token value including URL-encoded characters (%XX)
    const tokenMatch = body.match(/[?&]token=([a-zA-Z0-9_%-]+)/);
    return tokenMatch ? tokenMatch[1] : null;
  }

  extractInvitationToken(message: MailHogMessage): string | null {
    const body = this.decodeQuotedPrintable(message.Content.Body);
    // Match token value from register link (/register?token=...)
    const tokenMatch = body.match(/\/register\?token=([a-zA-Z0-9_%-]+)/);
    return tokenMatch ? tokenMatch[1] : null;
  }

  async getInvitationToken(email: string): Promise<string> {
    const message = await this.waitForEmail(email, 'invited');
    const token = this.extractInvitationToken(message);
    if (!token) {
      throw new Error(`Could not extract invitation token from email to ${email}`);
    }
    return token;
  }

  async waitForEmail(email: string, subject: string, timeoutMs: number = 30000): Promise<MailHogMessage> {
    const startTime = Date.now();

    while (Date.now() - startTime < timeoutMs) {
      const messages = await this.getMessagesForEmail(email);
      const targetMessage = messages.find((msg) =>
        msg.Content.Headers.Subject.some((s) => s.includes(subject))
      );

      if (targetMessage) {
        return targetMessage;
      }

      await new Promise((resolve) => setTimeout(resolve, 500));
    }

    throw new Error(`Timeout waiting for email to ${email} with subject "${subject}"`);
  }

  async deleteAllMessages(): Promise<void> {
    await fetch(`${this.baseUrl}/api/v1/messages`, { method: 'DELETE' });
  }

  async getVerificationToken(email: string): Promise<string> {
    const message = await this.waitForEmail(email, 'Verify');
    const token = this.extractVerificationToken(message);
    if (!token) {
      throw new Error(`Could not extract verification token from email to ${email}`);
    }
    return token;
  }
}
